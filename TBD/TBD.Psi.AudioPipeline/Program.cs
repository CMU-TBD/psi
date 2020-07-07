using System;
using System.IO;
using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Speech;

namespace TBD.Psi.AudioPipeline
{
    class Program
    {
        static void Main(string[] args)
        {


            // Get the directory name from user input
            Console.WriteLine("Enter the directory name >> ");
            var directoryName = Console.ReadLine();
            Console.WriteLine("Ok, directory name set to " + directoryName);

            // Create the directory in the Audio folder
            System.IO.Directory.CreateDirectory(@"C:\Data\Audio\" + directoryName);

            // Get the path for the input file
            Console.WriteLine("Please enter the path to the input file >> ");
            var inputPath = Console.ReadLine();
            Console.WriteLine("Ok, input file path set to " + inputPath);

            using (var p = Pipeline.Create(true))
            {
                // create a store for the data
                var store = Store.Create(p, "testStore", @"C:\Data\Stores");

                // get the audio file input
                var source = new WaveFileAudioSource(p, inputPath);

                // resample the audio to a different format
                var resampler = new AudioResampler(
                    p,
                    new AudioResamplerConfiguration()
                    {
                        OutputFormat = WaveFormat.Create16kHz1Channel16BitPcm()
                    });

                source.Out.PipeTo(resampler.In);

                // set up the audio 
                var audioVAD = new SystemVoiceActivityDetector(p);

                // send the mic audio to the VAD for voice deteaction
                resampler.Out.PipeTo(audioVAD.In);

                // join the voice detection stream with audio from mic
                var fused = audioVAD.Out.Join(resampler.Out);

                // write the whole audiobuffer to a file called full-output.wav
                var audioFileWriter = new WaveFileWriter(p, @"C:\Data\Audio\" + directoryName + @"\full-output.wav");
                resampler.Out.PipeTo(audioFileWriter);

                // init variables
                var counter = 0;    // keeps track of number of audio files
                var previousStateVoiceDetected = false;
                WaveDataWriterClass writer = null;

                // process the data stream containing the audio buffer and vad info
                var process = fused.Process<(bool, AudioBuffer), (bool, AudioBuffer)>(
                    (x, e, o) =>
                    {
                        // create variables for readability (extracted from message)
                        var voiceDetected = x.Item1;
                        var audio = x.Item2;

                        // if new voice detected, create new audio file
                        if (voiceDetected && !previousStateVoiceDetected)
                        {
                            Console.WriteLine("New Voice Detected ", counter);
                            writer = new WaveDataWriterClass(new FileStream(@"C:/Data/Audio/" + directoryName + @"/output-" + counter.ToString() + @".wav", FileMode.Create), audio.Format);
                            previousStateVoiceDetected = true;
                        }
                        // if same voice, continue adding to the audio file
                        else if (voiceDetected && previousStateVoiceDetected)
                        {
                            previousStateVoiceDetected = true;
                            writer.Write(audio.Data);
                        }
                        // if voice jus stopped than add to the counter and get rid of the writer
                        else if (!voiceDetected && previousStateVoiceDetected)
                        {
                            Console.WriteLine("Voice Stoped");
                            previousStateVoiceDetected = false;
                            counter++;
                            writer.Dispose();
                        }
                        // if no one is speaking, than do nothing
                        else if (!voiceDetected && !previousStateVoiceDetected)
                        {
                            previousStateVoiceDetected = false;
                        }
                    });


                // write to store for debugging
                resampler.Write("Audio", store);
                audioVAD.Write("AudioVAD", store);
                fused.Write("Joined", store);
                p.Diagnostics.Write("D", store);

                // Run the pipeline
                p.Run();

            }
        }
    }
}
