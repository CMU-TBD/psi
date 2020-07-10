using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Speech;
using Microsoft.VisualBasic.FileIO;

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

            // Get the script number the person is reading from
            Console.WriteLine("What script number is the person reading from >> ");
            var scriptNum = Console.ReadLine();
            Console.WriteLine("Ok, script number set to " + scriptNum);

            // Create the directory in the Audio folder
            System.IO.Directory.CreateDirectory(@"C:\Data\Audio\" + directoryName);

            // Get the path for the input file
            Console.WriteLine("Please enter the path to the input file >> ");
            var inputPath = Console.ReadLine();
            Console.WriteLine("Ok, input file path set to " + inputPath);

            Console.WriteLine("\nProcessing...");

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
                var audioFileWriter = new WaveFileWriter(p, @"C:\Data\Audio\" + directoryName + @"\" + directoryName + "-full-script" + scriptNum + ".wav");
                resampler.Out.PipeTo(audioFileWriter);

                // init variables
                var counter = 0;    // keeps track of number of audio files
                var previousStateVoiceDetected = false;
                var delayCounter = 0; // allows for buffer after vad stops detecting speech
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
                            writer = new WaveDataWriterClass(new FileStream(@"C:/Data/Audio/" + directoryName + @"/" + directoryName + "-script" + scriptNum + "-" + counter.ToString() + @".wav", FileMode.Create), audio.Format);
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
                            if (delayCounter < 30)
                            {
                                delayCounter++;
                                writer.Write(audio.Data);
                                Console.WriteLine("++");
                            }
                            else
                            {
                                delayCounter = 0;
                                Console.WriteLine("Voice Stoped");
                                previousStateVoiceDetected = false;
                                counter++;
                                writer.Dispose();
                            }

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

                // start building the csv output
                List<string> phrases = new List<string>();
                var parser = new TextFieldParser(@"C:/Data/CSVData/script" + scriptNum.ToString() + ".csv");

                // add each phrase to a list
                while (!parser.EndOfData)
                {
                    var phrase = parser.ReadLine();
                    phrases.Add(phrase);
                }

                // get all of the script files in order and store in array
                var recordingFiles = Directory.GetFiles(@"C:/Data/Audio/" + directoryName, directoryName + "-script*", System.IO.SearchOption.TopDirectoryOnly).OrderBy(f => new FileInfo(f).CreationTime);

                // add the size of each file to this list
                List<string> sizes = new List<string>();

                // create a csv
                var csv = new StringBuilder();
                var header = "wav_filename,wav_filesize,transcript";
                csv.AppendLine(header);

                

                // for each recording add a line to the output csv
                for (int i = 0; i < recordingFiles.Count(); i++)
                {
                    // get the size of the file
                    sizes.Add(new FileInfo(recordingFiles.ElementAt(i)).Length.ToString());

                    // if the is a mismatch between phrases and recordings, then send an alert so that they can be manually fixed
                    try
                    {
                        var newLine = phrases.ElementAt(i) + "," + sizes.ElementAt(i) + "," + recordingFiles.ElementAt(i);
                        csv.AppendLine(newLine);
                    }
                    catch
                    {
                        Console.WriteLine("");
                    }
                    
                }

                if (recordingFiles.Count() != phrases.Count)
                {
                    Console.WriteLine("VAD Error. Please manually fix files.");
                    Console.WriteLine(recordingFiles.Count().ToString());
                }

                // write the csv to file
                File.WriteAllText(@"C:\\Data\\Audio\\" + directoryName + @"\\" + directoryName + ".csv", csv.ToString());


                // keeps window open until keypress
                Console.WriteLine("\nFinished processing audio file. Press any key to exit.");
                Console.ReadLine();

            }
        }
    }
}
