using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace NAudio.CoreAudioApi
{
    public class WasapiLoopbackCapture : IWaveIn
    {
        private const long REFTIMES_PER_SEC = 10000000;
        private const long REFTIMES_PER_MILLISEC = 10000;
        private volatile bool stop;
        private byte[] recordBuffer;
        private Thread captureThread;
        private AudioClient audioClient;
        private int bytesPerFrame;
                
        public event EventHandler<WaveInEventArgs> DataAvailable;
                
        public event EventHandler<StoppedEventArgs> RecordingStopped;

        public WasapiLoopbackCapture() : this(GetDefaultCaptureDevice())
        {
        }
        
        public WasapiLoopbackCapture(MMDevice captureDevice)
        {
            this.audioClient = captureDevice.AudioClient;
        }
                
        public WaveFormat WaveFormat
        {
            get
            {
                return audioClient.MixFormat;
            }
            set
            {
                throw new Exception("Setting of Wave Format not supported for loopback device !");
            }
        }
                
        public static MMDevice GetDefaultCaptureDevice()
        {
            MMDeviceEnumerator devices = new MMDeviceEnumerator();
            return devices.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }

        private void InitializeCaptureDevice()
        {
            long requestedDuration = REFTIMES_PER_MILLISEC * 100;

            audioClient.Initialize(AudioClientShareMode.Shared,
                AudioClientStreamFlags.Loopback,
                requestedDuration,
                0,
                WaveFormat,
                Guid.Empty);

            int bufferFrameCount = audioClient.BufferSize;
            bytesPerFrame = WaveFormat.BlockAlign;
            recordBuffer = new byte[bufferFrameCount * bytesPerFrame];
            Debug.WriteLine(string.Format("record buffer size = {0}", recordBuffer.Length));
        }
        
        public void StartRecording()
        {
            InitializeCaptureDevice();
            ThreadStart start = delegate { this.CaptureThread(this.audioClient); };
            this.captureThread = new Thread(start);

            Debug.WriteLine("Thread starting...");
            this.stop = false;
            this.captureThread.Start();
        }

        public void StopRecording()
        {
            if (this.captureThread != null)
            {
                this.stop = true;

                Debug.WriteLine("Thread ending...");

                // wait for thread to end
                this.captureThread.Join();
                this.captureThread = null;

                Debug.WriteLine("Done.");

                this.stop = false;
            }
        }

        private void CaptureThread(AudioClient client)
        {
            Debug.WriteLine(client.BufferSize);
            int bufferFrameCount = audioClient.BufferSize;

            // Calculate the actual duration of the allocated buffer.
            long actualDuration = (long)((double)REFTIMES_PER_SEC * bufferFrameCount / WaveFormat.SampleRate);
            int sleepMilliseconds = (int)(actualDuration / REFTIMES_PER_MILLISEC / 2);

            AudioCaptureClient capture = client.AudioCaptureClient;
            client.Start();

            try
            {
                Debug.WriteLine(string.Format("sleep: {0} ms", sleepMilliseconds));
                while (!this.stop)
                {
                    Thread.Sleep(sleepMilliseconds);
                    ReadNextPacket(capture);
                }

                client.Stop();

                if (RecordingStopped != null)
                {
                    RecordingStopped(this, null);
                }
            }
            finally
            {
                if (capture != null)
                {
                    capture.Dispose();
                }
                if (client != null)
                {
                    client.Dispose();
                }

                client = null;
                capture = null;
            }

            System.Diagnostics.Debug.WriteLine("stop wasapi");
        }

        private void ReadNextPacket(AudioCaptureClient capture)
        {
            IntPtr buffer;
            int framesAvailable;
            AudioClientBufferFlags flags;
            int packetSize = capture.GetNextPacketSize();
            int recordBufferOffset = 0;
            //Debug.WriteLine(string.Format("packet size: {0} samples", packetSize / 4));

            while (packetSize != 0)
            {
                buffer = capture.GetBuffer(out framesAvailable, out flags);

                int bytesAvailable = framesAvailable * bytesPerFrame;

                //Debug.WriteLine(string.Format("got buffer: {0} frames", framesAvailable));

                // if not silence...
                if ((flags & AudioClientBufferFlags.Silent) != AudioClientBufferFlags.Silent)
                {
                    Marshal.Copy(buffer, recordBuffer, recordBufferOffset, bytesAvailable);
                }
                else
                {
                    Array.Clear(recordBuffer, recordBufferOffset, bytesAvailable);
                }
                recordBufferOffset += bytesAvailable;
                capture.ReleaseBuffer(framesAvailable);
                packetSize = capture.GetNextPacketSize();
            }


            if (DataAvailable != null)
            {
                DataAvailable(this, new WaveInEventArgs(recordBuffer, recordBufferOffset));
            }
        }

        public void Dispose()
        {
            StopRecording();
        }

        event EventHandler<StoppedEventArgs> IWaveIn.RecordingStopped
        {

            add
            {
                RecordingStopped += value;
            }
            remove
            {
                RecordingStopped -= value;
            }

        }
    }

}
