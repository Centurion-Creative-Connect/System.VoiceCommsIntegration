using CenturionCC.System.Player;
using DerpyNewbie.Common;
using DerpyNewbie.VoiceComms;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.VoiceCommsIntegration
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VoiceCommsIntegrator : PlayerManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private VoiceCommsManager voiceComms;

        [SerializeField] [Tooltip("Staff Team Id specified in PlayerManager")]
        private int staffTeamId = 255;
        [SerializeField] [Tooltip("If checked, Staff team VC will broadcast to all players")]
        private bool makeStaffTeamAsBroadcastChannel = true;

        private void Start()
        {
            playerManager.SubscribeCallback(this);
        }

        public override void OnLocalPlayerChanged(PlayerBase playerNullable, int index)
        {
            // Reset channels
            voiceComms._ClearRxChannel();
            voiceComms._ClearTxChannel();

            var channelId = playerNullable == null ? 0 : playerNullable.TeamId;
            voiceComms._AddRxChannel(channelId);
            voiceComms._AddTxChannel(channelId);

            if (makeStaffTeamAsBroadcastChannel) voiceComms._AddRxChannel(staffTeamId);
        }

        public override void OnTeamChanged(PlayerBase player, int oldTeam)
        {
            if (!player.IsLocal) return;

            // Remove previous team channel
            voiceComms._RemoveRxChannel(oldTeam);
            voiceComms._RemoveTxChannel(oldTeam);

            // Add current team channel
            var channelId = player.TeamId;
            voiceComms._AddRxChannel(channelId);
            voiceComms._AddTxChannel(channelId);

            if (makeStaffTeamAsBroadcastChannel && !voiceComms.RxChannelId.Contains(staffTeamId))
            {
                voiceComms._AddRxChannel(staffTeamId);
            }
        }
    }
}