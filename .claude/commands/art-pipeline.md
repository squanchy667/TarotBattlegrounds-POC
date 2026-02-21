---
description: Generate card art prompts and upload artwork to S3 via DevZone. Takes card name or scope as argument (e.g., "The Star" or "all Stars" or "all").
---

# Art Pipeline

Generate and manage card artwork for Tarot Battlegrounds.

Target: $ARGUMENTS (e.g., "The Star", "all Stars", "all tier 5")

## Step 1: Identify Cards

Read card data:
- `TarotBattlegrounds-POC/TarotBattlegrounds-POC/Assets/Scripts/Cards/CardDatabase.cs`
- DevZone cards.json (via S3 or local)

Filter cards matching $ARGUMENTS. List cards that need artwork (imageUrl is empty or missing).

## Step 2: Generate Art Prompts

For each card, generate an art prompt suitable for AI image generation:

**Prompt template:**
```
Tarot card illustration, [card name], [tribe] theme.
[Description based on card ability and flavor text].
Style: mystical tarot art, rich colors, ornate border details.
[Tribe-specific style notes]:
- Pentacles: earthy, golden, nature motifs
- Cups: aquatic, flowing, emotional
- Swords: metallic, sharp, dramatic
- Wands: fiery, energetic, magical
- Stars: cosmic, celestial, ethereal purple/indigo
- Coins: wealthy, amber, luxurious gold
Aspect ratio: 3:4 (portrait card format)
```

Present all prompts to the user.

## Step 3: User Provides Art

This step requires the user to:
1. Take the prompts to an image generation tool (Midjourney, DALL-E, etc.)
2. Generate images at 512x768 or higher
3. Provide file paths to the generated images

Wait for user to provide image paths.

## Step 4: Upload via DevZone

For each image provided:
1. Upload to S3 via DevZone card editor API:
   ```bash
   curl -X POST https://[devzone-api]/cards/[card-id]/image \
     -F "file=@[image-path]" \
     -H "Authorization: Bearer [token]"
   ```
2. Or use DevZone UI: open card in editor, upload via image field

## Step 5: Verify

1. Check S3 URLs are accessible
2. Verify game loads card art via RuntimeDataLoader
3. Report cards with art vs cards still missing art

## Output

- Cards processed: X
- Art prompts generated: X
- Images uploaded: X
- Cards still missing art: X (list)
