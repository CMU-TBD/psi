// Copyright (c) Carnegie Mellon University. All rights reserved.
// Licensed under the MIT license.

// Modified from psi\Sources\Integrations\CognitiveServices\Microsoft.Psi.CognitiveServices.Speech\AzureSpeechRecognizer.cs
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace TBD.Psi.DeepSpeech
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using DeepSpeechClient.Models;
    using DeepSpeechClient.Interfaces;
    using Microsoft.Psi;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Speech;

    public class DeepSpeechRecognizer : IConsumerProducer<ValueTuple<AudioBuffer, bool>, IStreamingSpeechRecognitionResult>, IDisposable
    {
        private readonly IDeepSpeech deepSpeechClient;
        private DeepSpeechStream deepSpeechStream;
        private DateTime audioStartingTime;
        private DateTime lastAudioOriginatingTime;
        private DateTime lastAudioContainingSpeechTime;
        private bool lastAudioContainedSpeech;
        private Queue<ValueTuple<AudioBuffer, bool>> currentQueue = new Queue<ValueTuple<AudioBuffer, bool>>();

        public Receiver<(AudioBuffer, bool)> In { get; private set; }

        public Emitter<IStreamingSpeechRecognitionResult> Out { get; private set; }

        public DeepSpeechRecognizer(Pipeline pipeline, string pathToModel, string pathToScorer = null)
        {
            // Create the Emitter and Receivers
            this.In = pipeline.CreateReceiver< ValueTuple<AudioBuffer, bool>>(this, this.Receive, nameof(this.In));
            this.Out = pipeline.CreateEmitter<IStreamingSpeechRecognitionResult>(this, nameof(this.Out));

            // Create the clinet
            this.deepSpeechClient = new DeepSpeechClient.DeepSpeech(pathToModel);
            // if path to scorer is provided, use 
            if (pathToScorer != null)
            {
                this.deepSpeechClient.EnableExternalScorer(pathToScorer);
            }
        }

        // Modified from ReceiveAsync in AzureSpeechRecognizer.cs
        protected void Receive(ValueTuple<AudioBuffer, bool> data, Envelope e)
        {

            byte[] audioData = data.Item1.Data;
            bool hasSpeech = data.Item2;

            if (this.lastAudioOriginatingTime == default)
            {
                this.lastAudioOriginatingTime = e.OriginatingTime - data.Item1.Duration;
            }

            var previousAudioOriginatingTime = this.lastAudioOriginatingTime;
            this.lastAudioOriginatingTime = e.OriginatingTime;

            // If the current sample has speech
            if (hasSpeech)
            {
                this.lastAudioContainingSpeechTime = e.OriginatingTime;

                // First time seeing a speech signal
                if (!this.lastAudioContainedSpeech)
                {
                    // Create a new audio stream
                    this.deepSpeechStream = this.deepSpeechClient.CreateStream();
                    this.audioStartingTime = e.OriginatingTime;
                }

                // DeepSpeech requires a short[] format. 
                // Used method described here: https://stackoverflow.com/questions/1104599/convert-byte-array-to-short-array-in-c-sharp
                short[] sAudioData = new short[(int)Math.Ceiling(audioData.Length / 2.0)];
                Buffer.BlockCopy(audioData, 0, sAudioData, 0, audioData.Length);

                // Add the audio data to the stream
                this.deepSpeechClient.FeedAudioContent(this.deepSpeechStream, sAudioData, Convert.ToUInt32(sAudioData.Length));

                // Add audio to the current utterance queue so we can reconstruct it in the recognition result later
                this.currentQueue.Enqueue(data.DeepClone(this.In.Recycler));
            }

            // If this is the last audio packet containing speech
            if (!hasSpeech && this.lastAudioContainedSpeech)
            {

                // get deep speech result
                var transcription = this.deepSpeechClient.FinishStream(this.deepSpeechStream);

                // Allocate a buffer large enough to hold the buffered audio
                BufferWriter bw = new BufferWriter(this.currentQueue.Sum(b => b.Item1.Length));

                // Get the audio associated with the recognized text from the current queue.
                ValueTuple<AudioBuffer, bool> buffer;
                while (this.currentQueue.Count > 0)
                {
                    buffer = this.currentQueue.Dequeue();
                    bw.Write(buffer.Item1.Data);

                    // We are done with this buffer so enqueue it for recycling
                    this.In.Recycle(buffer);
                }

                // Save the buffered audio
                var speechAudioData = new AudioBuffer(bw.Buffer, WaveFormat.Create16kHz1Channel16BitPcm());

                // create a new Utterance Object
                var result = new StreamingSpeechRecognitionResult(true, transcription, audio: speechAudioData, duration: (this.lastAudioContainingSpeechTime - this.audioStartingTime));
                this.Out.Post(result, this.audioStartingTime);
            }
            // Remember last audio state.
            this.lastAudioContainedSpeech = hasSpeech;
        }


        public void Dispose()
        {
            if (this.deepSpeechClient != null)
            {
                this.deepSpeechClient.Dispose();
                if (this.deepSpeechStream != null)
                {
                    this.deepSpeechStream.Dispose();
                }
            }
        }
    }
}
