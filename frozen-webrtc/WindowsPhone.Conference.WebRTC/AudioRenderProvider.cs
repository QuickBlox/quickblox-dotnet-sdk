using FM.IceLink.WebRTC;
using Microsoft.Xna.Framework.Audio;
using System;

namespace WindowsPhone.Conference.WebRTC
{
    class AudioRenderProvider : FM.IceLink.WebRTC.AudioRenderProvider
    {
        private DynamicSoundEffectInstance Playback;

        public override void Initialize(AudioRenderInitializeArgs renderArgs)
        {
            Playback = new DynamicSoundEffectInstance(ClockRate, Channels == 2 ? AudioChannels.Stereo : AudioChannels.Mono);
            Playback.Play();
        }

        public override void Render(AudioBuffer frame)
        {
            Playback.SubmitBuffer(frame.Data, frame.Index, frame.Length);
        }

        public override void Destroy()
        {
            Playback.Stop(true);
            Playback.Dispose();
        }
    }
}
