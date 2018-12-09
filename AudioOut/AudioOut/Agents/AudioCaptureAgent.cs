using MathNet.Numerics.IntegralTransforms;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AudioOut.Agents
{
    public class AudioCaptureAgent
    {
        private NAudio.CoreAudioApi.WasapiLoopbackCapture _capture;
        private AudioCaptureProcessor _processor;

        public AudioCaptureAgent()
        {
            _capture = new NAudio.CoreAudioApi.WasapiLoopbackCapture();
            _processor = new AudioCaptureProcessor();
            _capture.RecordingStopped += AudioCaptureProcessor.handleEndOfAudio;
            _capture.DataAvailable += _processor.handleAudioData;
            _capture.StartRecording();
        }
    }

    public class AudioCaptureProcessor
    {
        private const int vlb = 50;
        private const int mb = 150;
        private const int hb = 300;

        private const int lmr = 1000;
        private const int mmr = 3000;
        private const int hmr = 5000;

        private const int tr = 10000;

        private Complex[] buffer = new Complex[1024];
        private int currentIdx = 0;


        public static void handleEndOfAudio(object sender, StoppedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("Handled");
        }


        public void handleAudioData(object sender, WaveInEventArgs args)
        {
            NAudio.CoreAudioApi.WasapiLoopbackCapture capture = (NAudio.CoreAudioApi.WasapiLoopbackCapture)sender;

            //left channel = first 4 bytes;
            //right channel = next 4;
            for (int i = 0; i < args.BytesRecorded; i += 8)
            {
                if (currentIdx < buffer.Length)
                {
                    float left = BitConverter.ToSingle(args.Buffer, i);
                    float right = BitConverter.ToSingle(args.Buffer, i + 4);
                    float combined = (left + right) / 2;
                    buffer[currentIdx++] = new System.Numerics.Complex(combined, 0);
                }
                else
                {
                    currentIdx = 0;
                    int sampleRate = capture.WaveFormat.SampleRate;
                    Fourier.Radix2Forward(buffer, FourierOptions.Default);
                    double BASS = 0;
                    double MIDS = 0;
                    double TREBS = 0;

                    for (int m = 0; m < buffer.Length; m++)
                    {
                        double f_m = m / (buffer.Length / (double)sampleRate);

                        if (f_m < vlb)
                        {
                            BASS += buffer[m].Magnitude;
                        }
                        else if (f_m < mb)
                        {
                            BASS += buffer[m].Magnitude;
                        }
                        else if (f_m < hb)
                        {
                            BASS += buffer[m].Magnitude;
                        }
                        else if (f_m < lmr)
                        {
                            MIDS += buffer[m].Magnitude;
                        }
                        else if (f_m < mmr)
                        {
                            MIDS += buffer[m].Magnitude;
                        }
                        else if (f_m < hmr)
                        {
                            MIDS += buffer[m].Magnitude;
                        }
                        else if (f_m < tr)
                        {
                            TREBS += buffer[m].Magnitude;
                        }
                        else
                        {
                            break;
                        }

                    }

                    //if (FrequencyDataAvailable != null)
                    //{
                    //    FrequencyDataAvailable(this, new FrequencyEventArgs(new FrequencyInfo(BASS, MIDS, TREBS)));
                    //}
                }
            }

        }
    }
}
