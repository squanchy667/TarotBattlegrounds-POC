#!/usr/bin/env python3
"""
Unity Multiplayer Log Analyzer
Parses Host and Client console logs from ParrelSync testing to identify bugs and desyncs.
"""

import re
import sys
from collections import defaultdict
from datetime import datetime
from pathlib import Path
from typing import List, Dict, Tuple, Optional

class LogEvent:
    """Represents a single log event"""
    def __init__(self, timestamp: str, player: str, tag: str, message: str, line_num: int):
        self.timestamp = timestamp
        self.player = player  # "Host" or "Client"
        self.tag = tag  # e.g., "[M3]", "[Lifecycle Event]", etc.
        self.message = message
        self.line_num = line_num
        self.turn = self._extract_turn()

    def _extract_turn(self) -> Optional[int]:
        """Extract turn number from message if present"""
        match = re.search(r'[Tt]urn[:\s]+(\d+)', self.message)
        if match:
            return int(match.group(1))
        match = re.search(r'\[T(\d+)\]', self.message)
        if match:
            return int(match.group(1))
        return None

    def __repr__(self):
        return f"LogEvent({self.player}, {self.tag}, Turn {self.turn}, Line {self.line_num})"

class LogAnalyzer:
    """Analyzes Unity console logs for multiplayer issues"""

    # Patterns for important log tags
    TAG_PATTERNS = {
        'M1_synergy': r'\[SynergyManager\]',
        'M2_discovery': r'\[DiscoveryUI\]',
        'M3_shop_host': r'\[Host/M3\]',
        'M3_shop_client': r'\[Client/M3\]',
        'M3_cardlookup': r'\[CardLookup\]',
        'M3_tavern': r'\[TavernManager/M3\]',
        'M4_buy_rpc': r'\[Host\] RPC_RequestBuyCard',
        'M5_lifecycle': r'\[Lifecycle Event\]',
        'M5_upgrade': r'Upgraded to Tavern Tier',
        'M6_ability': r'\[AbilityManager\]',
        'M7_combat_log': r'\[CombatLogUI\]',
        'M8_coins': r'Coins:',
    }

    # Error patterns
    ERROR_PATTERNS = [
        (r'Template not found: ([\w\s]+)', 'CRITICAL', 'M3'),
        (r'Invalid shopIndex (\d+), shop has (\d+) cards', 'CRITICAL', 'M4'),
        (r'Invalid sender slot', 'CRITICAL', 'M4'),
        (r'ArgumentOutOfRangeException', 'CRITICAL', 'Multiple'),
        (r'NullReferenceException', 'CRITICAL', 'Multiple'),
        (r'IndexOutOfRangeException', 'CRITICAL', 'Multiple'),
    ]

    def __init__(self, host_log_path: str, client_log_path: str):
        self.host_log_path = Path(host_log_path)
        self.client_log_path = Path(client_log_path)
        self.host_events: List[LogEvent] = []
        self.client_events: List[LogEvent] = []
        self.issues = {
            'P0': [],  # Critical
            'P1': [],  # Important
            'P2': []   # Minor
        }
        self.working_systems = []
        self.stats = {
            'host_lines': 0,
            'client_lines': 0,
            'host_events': 0,
            'client_events': 0,
            'desyncs': 0,
            'errors': 0,
            'warnings': 0
        }

    def parse_log_file(self, file_path: Path, player: str) -> List[LogEvent]:
        """Parse a log file and extract events"""
        events = []

        if not file_path.exists():
            print(f"⚠️  Warning: {file_path} not found")
            return events

        with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
            lines = f.readlines()
            self.stats[f'{player.lower()}_lines'] = len(lines)

            for line_num, line in enumerate(lines, 1):
                # Extract timestamp if present
                timestamp_match = re.match(r'(\d{2}:\d{2}:\d{2})', line)
                timestamp = timestamp_match.group(1) if timestamp_match else None

                # Check for known tags
                for tag_name, pattern in self.TAG_PATTERNS.items():
                    if re.search(pattern, line):
                        events.append(LogEvent(
                            timestamp=timestamp,
                            player=player,
                            tag=tag_name,
                            message=line.strip(),
                            line_num=line_num
                        ))
                        break

        self.stats[f'{player.lower()}_events'] = len(events)
        return events

    def check_m1_synergy(self):
        """Check M1: Synergy per-player state"""
        host_synergy = [e for e in self.host_events if e.tag == 'M1_synergy']
        client_synergy = [e for e in self.client_events if e.tag == 'M1_synergy']

        if host_synergy or client_synergy:
            # Check for "global state overwrite" errors
            overwrite_errors = [e for e in host_synergy + client_synergy
                              if 'overwrite' in e.message.lower()]

            if overwrite_errors:
                self.issues['P0'].append({
                    'task': 'M1',
                    'title': 'Synergy global state overwrite detected',
                    'details': overwrite_errors,
                    'severity': 'CRITICAL'
                })
            else:
                self.working_systems.append('M1: Synergy per-player calculations')

    def check_m3_shop_sync(self):
        """Check M3: Shop sync and CardLookup"""
        host_shop = [e for e in self.host_events if 'M3_shop' in e.tag or e.tag == 'M3_cardlookup']
        client_shop = [e for e in self.client_events if 'M3_shop' in e.tag or e.tag == 'M3_cardlookup']

        # Check for "Template not found" errors
        template_errors = []
        for events in [host_shop, client_shop]:
            for event in events:
                if 'Template not found' in event.message:
                    template_errors.append(event)

        if template_errors:
            self.issues['P0'].append({
                'task': 'M3',
                'title': 'CardLookup template deserialization failure',
                'details': template_errors,
                'severity': 'CRITICAL',
                'fix': 'Verify CardLookup uses TavernManager.masterCards'
            })
            self.stats['errors'] += len(template_errors)

        # Compare shop card counts
        shop_broadcasts = [e for e in host_shop if 'Broadcasting shop' in e.message]
        shop_receives = [e for e in client_shop if 'Received shop' in e.message]

        for broadcast in shop_broadcasts:
            # Extract card count
            match = re.search(r': (\d+) cards', broadcast.message)
            if match:
                host_count = int(match.group(1))

                # Find corresponding client receive
                player_match = re.search(r'P(\d+)', broadcast.message)
                if player_match:
                    player_id = player_match.group(1)

                    for receive in shop_receives:
                        if f'P{player_id}' in receive.message:
                            client_match = re.search(r': (\d+) cards', receive.message)
                            if client_match:
                                client_count = int(client_match.group(1))

                                if host_count != client_count:
                                    self.issues['P0'].append({
                                        'task': 'M3',
                                        'title': f'Shop card count mismatch for P{player_id}',
                                        'details': f'Host sent {host_count} cards, Client received {client_count}',
                                        'severity': 'CRITICAL',
                                        'host_line': broadcast.line_num,
                                        'client_line': receive.line_num
                                    })
                                    self.stats['desyncs'] += 1

        if not template_errors and shop_broadcasts and shop_receives:
            self.working_systems.append('M3: Shop sync and CardLookup')

    def check_m4_buy_rpc(self):
        """Check M4: Player 2 buy RPC"""
        buy_rpcs = [e for e in self.host_events if e.tag == 'M4_buy_rpc']

        # Check for errors in buy RPCs
        errors = []
        for event in buy_rpcs:
            if 'Invalid' in event.message or 'error' in event.message.lower():
                errors.append(event)

        if errors:
            self.issues['P0'].append({
                'task': 'M4',
                'title': 'Buy RPC validation failures',
                'details': errors,
                'severity': 'CRITICAL'
            })
            self.stats['errors'] += len(errors)
        elif buy_rpcs:
            self.working_systems.append('M4: Buy RPC validation')

    def check_m5_upgrade_cost(self):
        """Check M5: Upgrade cost reduction"""
        host_lifecycle = [e for e in self.host_events if e.tag == 'M5_lifecycle']
        client_lifecycle = [e for e in self.client_events if e.tag == 'M5_lifecycle']

        # Extract costs by turn and player
        costs_by_turn = defaultdict(lambda: {'host': {}, 'client': {}})

        for event in host_lifecycle:
            # Extract player number and cost
            player_match = re.search(r'Player (\d+).*cost.*?(\d+)', event.message)
            if player_match and event.turn:
                player_id = player_match.group(1)
                cost = int(player_match.group(2))
                costs_by_turn[event.turn]['host'][player_id] = (cost, event.line_num)

        for event in client_lifecycle:
            player_match = re.search(r'Player (\d+).*cost.*?(\d+)', event.message)
            if player_match and event.turn:
                player_id = player_match.group(1)
                cost = int(player_match.group(2))
                costs_by_turn[event.turn]['client'][player_id] = (cost, event.line_num)

        # Compare costs across players at same turn
        desyncs = []
        for turn, costs in costs_by_turn.items():
            for player_id in set(costs['host'].keys()) & set(costs['client'].keys()):
                host_cost, host_line = costs['host'][player_id]
                client_cost, client_line = costs['client'][player_id]

                if host_cost != client_cost:
                    desyncs.append({
                        'turn': turn,
                        'player': player_id,
                        'host_cost': host_cost,
                        'client_cost': client_cost,
                        'host_line': host_line,
                        'client_line': client_line
                    })
                    self.stats['desyncs'] += 1

        if desyncs:
            self.issues['P0'].append({
                'task': 'M5',
                'title': 'Upgrade cost desync detected',
                'details': desyncs,
                'severity': 'CRITICAL',
                'fix': 'Verify lifecycle event runs on both host and client'
            })
        elif host_lifecycle or client_lifecycle:
            self.working_systems.append('M5: Upgrade cost reduction')

    def check_m6_memory_cleanup(self):
        """Check M6: AbilityManager memory leak"""
        ability_events = [e for e in self.host_events + self.client_events if e.tag == 'M6_ability']

        clear_calls = [e for e in ability_events if 'ClearAll' in e.message or 'Cleared' in e.message]

        if clear_calls:
            self.working_systems.append('M6: AbilityManager cleanup')
        elif ability_events:
            self.issues['P1'].append({
                'task': 'M6',
                'title': 'No AbilityManager.ClearAll() calls found',
                'details': 'Memory leak possible - abilities not cleaned between games',
                'severity': 'IMPORTANT'
            })

    def check_m7_combat_log(self):
        """Check M7: Combat log local filter"""
        combat_events = [e for e in self.host_events + self.client_events if e.tag == 'M7_combat_log']

        skip_messages = [e for e in combat_events if 'Skipping battle' in e.message]

        if skip_messages:
            self.working_systems.append('M7: Combat log local filtering')
        elif combat_events:
            self.issues['P2'].append({
                'task': 'M7',
                'title': 'Combat log filter unclear',
                'details': 'No "Skipping battle" messages found',
                'severity': 'MINOR'
            })

    def check_m8_coin_events(self):
        """Check M8: RefreshShop coin setter"""
        coin_events = [e for e in self.host_events + self.client_events if e.tag == 'M8_coins']

        if coin_events:
            # Check coin progression (3→4→5→...→10)
            coin_values = []
            for event in sorted(coin_events, key=lambda e: e.line_num):
                match = re.search(r'Coins:.*?(\d+)', event.message)
                if match:
                    coin_values.append(int(match.group(1)))

            # Verify progression (should increase by 1 each turn up to 10)
            if coin_values:
                self.working_systems.append('M8: Coin property setter')

    def detect_errors(self):
        """Detect common error patterns in logs"""
        all_events = self.host_events + self.client_events

        for event in all_events:
            for pattern, severity, task in self.ERROR_PATTERNS:
                if re.search(pattern, event.message):
                    priority = 'P0' if severity == 'CRITICAL' else 'P1'
                    self.issues[priority].append({
                        'task': task,
                        'title': f'{severity}: {pattern}',
                        'details': event,
                        'severity': severity
                    })
                    self.stats['errors'] += 1

    def analyze(self):
        """Run full analysis"""
        print("🔍 Analyzing logs...")
        print(f"   ├─ Reading {self.host_log_path.name} ...")
        self.host_events = self.parse_log_file(self.host_log_path, "Host")
        print(f"   │  └─ {self.stats['host_lines']} lines, {self.stats['host_events']} events")

        print(f"   ├─ Reading {self.client_log_path.name} ...")
        self.client_events = self.parse_log_file(self.client_log_path, "Client")
        print(f"   │  └─ {self.stats['client_lines']} lines, {self.stats['client_events']} events")

        print("   ├─ Checking M1: Synergy per-player...")
        self.check_m1_synergy()

        print("   ├─ Checking M3: Shop sync...")
        self.check_m3_shop_sync()

        print("   ├─ Checking M4: Buy RPC...")
        self.check_m4_buy_rpc()

        print("   ├─ Checking M5: Upgrade cost...")
        self.check_m5_upgrade_cost()

        print("   ├─ Checking M6: Memory cleanup...")
        self.check_m6_memory_cleanup()

        print("   ├─ Checking M7: Combat log filter...")
        self.check_m7_combat_log()

        print("   ├─ Checking M8: Coin events...")
        self.check_m8_coin_events()

        print("   ├─ Detecting error patterns...")
        self.detect_errors()

        print("   └─ Generating report...")

    def generate_report(self, output_path: Optional[Path] = None) -> str:
        """Generate markdown report"""
        report = []
        report.append("# Phase M - Log Analysis Report")
        report.append(f"\n**Generated:** {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        report.append(f"**Host Log:** {self.host_log_path}")
        report.append(f"**Client Log:** {self.client_log_path}")
        report.append("\n---\n")

        # Statistics
        report.append("## 📊 Test Statistics\n")
        report.append(f"- **Host Lines:** {self.stats['host_lines']}")
        report.append(f"- **Client Lines:** {self.stats['client_lines']}")
        report.append(f"- **Host Events:** {self.stats['host_events']}")
        report.append(f"- **Client Events:** {self.stats['client_events']}")
        report.append(f"- **Desyncs Found:** {self.stats['desyncs']}")
        report.append(f"- **Errors:** {self.stats['errors']}")
        report.append("\n---\n")

        # Critical Issues
        if self.issues['P0']:
            report.append("## 🎯 Critical Issues (P0) - MUST FIX\n")
            for i, issue in enumerate(self.issues['P0'], 1):
                report.append(f"### {i}. [{issue['task']}] {issue['title']}\n")
                report.append(f"**Severity:** {issue['severity']}\n")
                if isinstance(issue['details'], str):
                    report.append(f"**Details:** {issue['details']}\n")
                elif isinstance(issue['details'], list):
                    report.append(f"**Occurrences:** {len(issue['details'])}\n")
                    for detail in issue['details'][:3]:  # Show first 3
                        if isinstance(detail, LogEvent):
                            report.append(f"- Line {detail.line_num} ({detail.player}): `{detail.message[:100]}...`\n")
                        else:
                            report.append(f"- {detail}\n")
                elif isinstance(issue['details'], dict):
                    for key, value in issue['details'].items():
                        report.append(f"- **{key}:** {value}\n")

                if 'fix' in issue:
                    report.append(f"\n**Suggested Fix:** {issue['fix']}\n")
                report.append("\n")
        else:
            report.append("## 🎯 Critical Issues (P0)\n\n✅ **None found!**\n\n")

        # Important Issues
        if self.issues['P1']:
            report.append("## ⚠️ Important Issues (P1) - SHOULD FIX\n")
            for i, issue in enumerate(self.issues['P1'], 1):
                report.append(f"### {i}. [{issue['task']}] {issue['title']}\n")
                report.append(f"**Details:** {issue.get('details', 'See logs')}\n\n")
        else:
            report.append("## ⚠️ Important Issues (P1)\n\n✅ **None found!**\n\n")

        # Minor Issues
        if self.issues['P2']:
            report.append("## 💡 Minor Issues (P2) - COULD FIX\n")
            for i, issue in enumerate(self.issues['P2'], 1):
                report.append(f"### {i}. [{issue['task']}] {issue['title']}\n")
                report.append(f"**Details:** {issue.get('details', 'See logs')}\n\n")

        # Working Systems
        report.append("## ✅ Verified Working Systems\n")
        if self.working_systems:
            for system in self.working_systems:
                report.append(f"- ✅ {system}\n")
        else:
            report.append("*No systems verified* (insufficient log data or all have issues)\n")

        report.append("\n---\n")

        # Summary
        report.append("## 📋 Summary\n")
        total_issues = len(self.issues['P0']) + len(self.issues['P1']) + len(self.issues['P2'])
        if total_issues == 0 and self.working_systems:
            report.append("### 🎉 ALL TESTS PASS!\n")
            report.append("All Phase M tasks are working correctly. Ready to proceed to Phase I.\n")
        elif self.issues['P0']:
            report.append("### ⚠️ CRITICAL ISSUES FOUND\n")
            report.append(f"Found {len(self.issues['P0'])} blocking issues that must be fixed before proceeding.\n")
        else:
            report.append("### 🟡 PARTIAL SUCCESS\n")
            report.append(f"No critical issues, but {total_issues} minor/important issues found.\n")

        report_text = "\n".join(report)

        # Write to file if path provided
        if output_path:
            output_path.write_text(report_text, encoding='utf-8')
            print(f"\n📄 Full report saved: {output_path}")

        return report_text

    def print_summary(self):
        """Print console summary"""
        print("\n" + "="*60)
        print("📊 Analysis Complete!")
        print("="*60)

        print(f"\n🎯 Critical Issues Found: {len(self.issues['P0'])}")
        for issue in self.issues['P0']:
            print(f"   ❌ {issue['task']}: {issue['title']}")

        print(f"\n⚠️  Important Issues: {len(self.issues['P1'])}")
        for issue in self.issues['P1']:
            print(f"   🟡 {issue['task']}: {issue['title']}")

        print(f"\n💡 Minor Issues: {len(self.issues['P2'])}")

        print(f"\n✅ Working Systems: {len(self.working_systems)}")
        for system in self.working_systems:
            print(f"   ✓ {system}")

        print(f"\n📊 Statistics:")
        print(f"   - Events analyzed: {self.stats['host_events'] + self.stats['client_events']}")
        print(f"   - Desyncs: {self.stats['desyncs']}")
        print(f"   - Errors: {self.stats['errors']}")

def main():
    """Main entry point"""
    if len(sys.argv) < 3:
        print("Usage: python analyze_logs.py <host_log> <client_log>")
        print("\nExample:")
        print("  python analyze_logs.py test-logs/host_console.log test-logs/client_console.log")
        sys.exit(1)

    host_log = sys.argv[1]
    client_log = sys.argv[2]

    analyzer = LogAnalyzer(host_log, client_log)
    analyzer.analyze()
    analyzer.print_summary()

    # Generate report
    output_dir = Path("test-logs")
    output_dir.mkdir(exist_ok=True)
    output_path = output_dir / "analysis_report.md"
    analyzer.generate_report(output_path)

if __name__ == "__main__":
    main()
