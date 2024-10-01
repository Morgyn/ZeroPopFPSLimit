using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace ZeroPopFPSLimitNS
{
    public class ZPFLLoader : IHarmonyModHooks

    {
        public void OnLoaded(OnHarmonyModLoadedArgs args)
        {
            if (!Bootstrap.isPresent)
            {
                Debug.Log($"0PFL: Loaded at boot");
                return;
            }

            ZeroPopFPSLimit.Initialize();

            ZeroPopFPSLimit.checkForZeroPlayers();
            Debug.Log($"0PFL: Dynamic Loaded Setup {ZeroPopFPSLimit.status()}");               
        }

        public void OnUnloaded(OnHarmonyModUnloadedArgs args)
        {
            ZeroPopFPSLimit.restoreFPSLimit();
            if (ZeroPopFPSLimit.Instance != null)
            {
                Object.DestroyImmediate(ZeroPopFPSLimit.Instance);
            }
            Debug.Log($"0PFL: Unoaded");
        }
    }

    public class ZeroPopFPSLimit : SingletonComponent<ZeroPopFPSLimit>
    {
        const float Tick_Interval = 300.0f;
        const int Low_FPS = 1;
        protected override void Awake()
        {
            base.Awake();
            Debug.Log($"0PFL: Awake");
            StartCoroutine(Tick());
        }

        internal static void Initialize()
        {
            Debug.Log($"0PFL: Initialize");
            new GameObject().AddComponent<ZeroPopFPSLimit>();     
        }

        IEnumerator Tick()
        {  
            while (true)
            {
                yield return new WaitForSeconds(Tick_Interval);
                Debug.Log($"0PFL: Tick {status()}");
                if (Application.targetFrameRate == Low_FPS)
                {
                    if (playersConnectedAndJoining() != 0)
                    {
                        restoreFPSLimit();
                    }
                }
                else
                {
                    if (playersConnectedAndJoining() == 0)
                    {
                        setLowFPSLimit();
                    }
                }
            }
        }

        public static int playersConnectedAndJoining()
        {
            return ConnectionAuth.m_AuthConnection.Count + BasePlayer.activePlayerList.Count + ServerMgr.Instance.connectionQueue.Joining;
        }

        public static string status()
        {
            return $"Players: {playersConnectedAndJoining()}, Current FPS Limit: {Application.targetFrameRate}, Configured Limit: {ConVar.FPS.limit}";
        }

        public static bool checkForZeroPlayers()
        {
            Debug.Log($"0PFL: Check for players {status()}");
            if (Application.targetFrameRate != Low_FPS)
            {
                if (playersConnectedAndJoining() == 0)
                {
                    setLowFPSLimit();
                    return true;
                }
            }
            return false;
        }

        public static bool newConnection()
        {
            Debug.Log($"0PFL: New Connection {status()}");
            if (Application.targetFrameRate == Low_FPS)
            {
                restoreFPSLimit();
                return true;
            }
            return false;
        }

        public static void setLowFPSLimit()
        {
            Application.targetFrameRate = Low_FPS;
            Debug.Log($"0PFL: Set FPS limit to {Low_FPS}");
        }

        public static void restoreFPSLimit()
        {
            Application.targetFrameRate = ConVar.FPS.limit;
            Debug.Log($"0PFL: Set FPS limit to {ConVar.FPS.limit}");
        }
    }

    #region Hooks
    [HarmonyPatch(typeof(ServerMgr), "OpenConnection")]
    public class GameSetup
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            Debug.Log($"0PFL: Server Start");
            ZeroPopFPSLimit.checkForZeroPlayers();
            ZeroPopFPSLimit.Initialize();
        }
    }

    [HarmonyPatch(typeof(BasePlayer), "OnDisconnected")]
    public class Disconnected
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            Debug.Log($"0PFL: Player Disconnected");
            ZeroPopFPSLimit.checkForZeroPlayers();
        }
    }
    [HarmonyPatch(typeof(ConnectionAuth), "OnNewConnection")]
    public class NewConnection
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            Debug.Log($"0PFL: New connection");
            ZeroPopFPSLimit.newConnection();
        }
    }
    #endregion
}