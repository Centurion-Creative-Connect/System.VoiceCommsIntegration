﻿using CenturionCC.System.Player;
using DerpyNewbie.Common;
using DerpyNewbie.VoiceComms;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace CenturionCC.System.VoiceCommsIntegration
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VoiceCommsIntegrator : PlayerManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private VoiceCommsManager voiceComms;

        [SerializeField]
        [Tooltip("Staff Team Id specified in PlayerManager")]
        private int staffTeamId = 255;
        [SerializeField]
        [Tooltip("If checked, Staff team VC will broadcast to all players")]
        private bool makeStaffTeamAsBroadcastChannel = true;
        [SerializeField]
        [Tooltip("If checked, Staff team will receive team VCs. defaults true")]
        [InspectorName("Make Staff Team Receive Team VC")]
        private bool makeStaffTeamReceiveTeamVc = true;
        [SerializeField]
        [Tooltip("List of player team ids to listen when in staff team. defaults {red:1, yellow:2, green:3, blue:4}")]
        private int[] staffTeamRxChannels = { 1, 2, 3, 4 };

        private void Start()
        {
            playerManager.SubscribeCallback(this);
        }

        public override void OnLocalPlayerChanged(PlayerBase playerNullable, int index)
        {
            _UpdateVoiceCommsChannels(playerNullable == null ? 0 : playerNullable.TeamId);
        }

        public override void OnTeamChanged(PlayerBase player, int oldTeam)
        {
            if (!player.IsLocal) return;

            _UpdateVoiceCommsChannels(player.TeamId);
        }

        public void _UpdateVoiceCommsChannels(int teamId)
        {
            _ClearRxChannel();
            _ClearTxChannel();

            // Add default Rx channel
            _AddRxChannel(0);

            // Add team VC Rx/Tx channel
            _AddRxChannel(teamId);
            _AddTxChannel(teamId);

            // Add staff broadcasting Rx channel if needed
            if (!playerManager.IsStaffTeamId(teamId) && makeStaffTeamAsBroadcastChannel)
            {
                _AddRxChannel(staffTeamId);
            }

            // Add staff Rx channel if needed
            if (playerManager.IsStaffTeamId(teamId) && makeStaffTeamReceiveTeamVc)
            {
                foreach (var rxChannel in staffTeamRxChannels) _AddRxChannel(rxChannel);
            }
        }

        #region VoiceCommsHandling

        // This is basically wrapper for VoiceComms to prevent conflict between any other gimmicks using VoiceComms

        private readonly DataList _rxChannels = new DataList();
        private readonly DataList _txChannels = new DataList();

        private void _AddTxChannel(int channelId)
        {
            voiceComms._AddTxChannel(channelId);
            _txChannels.Add(channelId);
        }

        private void _RemoveTxChannel(int channelId)
        {
            voiceComms._RemoveTxChannel(channelId);
            _txChannels.Remove(channelId);
        }

        private void _ClearTxChannel()
        {
            var tokens = _txChannels.ToArray();
            foreach (var token in tokens)
                voiceComms._RemoveTxChannel(token.Int);

            _txChannels.Clear();
        }

        private void _AddRxChannel(int channelId)
        {
            voiceComms._AddRxChannel(channelId);
            _rxChannels.Add(channelId);
        }

        private void _RemoveRxChannel(int channelId)
        {
            voiceComms._RemoveRxChannel(channelId);
            _rxChannels.Remove(channelId);
        }

        private void _ClearRxChannel()
        {
            var tokens = _rxChannels.ToArray();
            foreach (var token in tokens)
                voiceComms._RemoveRxChannel(token.Int);

            _rxChannels.Clear();
        }

        #endregion
    }
}