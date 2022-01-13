﻿// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// 
using System;
#if UNITY_WSA
using Microsoft.Xbox.Services;
using Microsoft.Xbox.Services.Social.Manager;

using UnityEngine;

namespace Microsoft.Xbox.Services.Client
{
    public delegate void SocialEventHandler(object sender, SocialEvent socialEvent);

    public class SocialManagerComponent : Singleton<SocialManagerComponent>
    {
        public event SocialEventHandler EventProcessed;

        private ISocialManager manager;

        protected SocialManagerComponent()
        {
        }

        /// <summary>
        /// Awake is called when the script instance is being loaded
        /// </summary>
        private void Awake()
        {
            this.manager = XboxLive.Instance.SocialManager;
        }

        private void Update()
        {
            try
            {
                var socialEvents = this.manager.DoWork();

                foreach (SocialEvent socialEvent in socialEvents)
                {
                    if (XboxLiveServicesSettings.Instance.DebugLogsOn)
                    {
                        Debug.LogFormat("[SocialManager] Processed {0} event.", socialEvent.EventType);
                    }
                    this.OnEventProcessed(socialEvent);
                }
            }
            catch (Exception ex)
            {
                ExceptionManager.Instance.ThrowException(
                        ExceptionSource.SocialManager,
                        ExceptionType.UnexpectedException,
                        ex);
            }
        }

        protected virtual void OnEventProcessed(SocialEvent socialEvent)
        {
            var handler = this.EventProcessed;
            if (handler != null) handler(this, socialEvent);
        }
    }
}
#endif