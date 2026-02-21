using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

namespace TarotBattlegrounds.Combat.Replay
{
    /// <summary>
    /// Serialization and deserialization of CombatReplay data for network transport.
    /// Supports JSON (human-readable) and GZip-compressed binary (for Photon RPCs).
    /// Version field ensures forward compatibility.
    /// </summary>
    public static class ReplaySerializer
    {
        private static readonly JsonSerializerSettings s_settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Include,
            TypeNameHandling = TypeNameHandling.None
        };

        public static string Serialize(CombatReplay replay)
        {
            if (replay == null)
                throw new ArgumentNullException(nameof(replay));

            var envelope = new ReplayEnvelope
            {
                schemaVersion = CombatReplay.CURRENT_VERSION,
                replay = replay
            };

            return JsonConvert.SerializeObject(envelope, s_settings);
        }

        public static CombatReplay Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[ReplaySerializer] Deserialize called with null/empty JSON.");
                return null;
            }

            try
            {
                var envelope = JsonConvert.DeserializeObject<ReplayEnvelope>(json, s_settings);
                if (envelope?.replay == null)
                {
                    Debug.LogError("[ReplaySerializer] Deserialized envelope or replay is null.");
                    return null;
                }

                if (envelope.schemaVersion > CombatReplay.CURRENT_VERSION)
                    Debug.LogWarning($"[ReplaySerializer] Replay schema v{envelope.schemaVersion} newer than supported v{CombatReplay.CURRENT_VERSION}.");

                return envelope.replay;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReplaySerializer] JSON deserialization failed: {ex.Message}");
                return null;
            }
        }

        public static byte[] SerializeCompressed(CombatReplay replay)
        {
            string json = Serialize(replay);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            using (var outputStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress, leaveOpen: true))
                {
                    gzipStream.Write(jsonBytes, 0, jsonBytes.Length);
                }
                return outputStream.ToArray();
            }
        }

        public static CombatReplay DeserializeCompressed(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                Debug.LogError("[ReplaySerializer] DeserializeCompressed called with null/empty data.");
                return null;
            }

            try
            {
                using (var inputStream = new MemoryStream(data))
                using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
                using (var reader = new StreamReader(gzipStream, Encoding.UTF8))
                {
                    string json = reader.ReadToEnd();
                    return Deserialize(json);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReplaySerializer] Decompression failed: {ex.Message}");
                return null;
            }
        }

        [Serializable]
        private class ReplayEnvelope
        {
            public int schemaVersion;
            public CombatReplay replay;
        }
    }
}
