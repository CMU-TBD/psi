using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBD.Psi.Playground.NETFramework
{
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Speech;
    using TBD.Psi.DeepSpeech;

    public class ProgramDeepSpeech
    {
        public static void Run()
        {
            using (var pipeline = Pipeline.Create())
            {
                var store = Store.Create(pipeline, "audioTest", @"C:\Data\Stores");
                var audioInput = new AudioCapture(pipeline, new AudioCaptureConfiguration());
                audioInput.Write("audio", store);

                // Create a VAD & pipe audio to it
                var vad = new SystemVoiceActivityDetector(pipeline);
                vad.Write("vad", store);
                audioInput.PipeTo(vad);

                // join the audio and VAD together
                var voice = audioInput.Join(vad);

                // pipe to the recognizer
                var recognizer = new DeepSpeechRecognizer(pipeline, @"C:\Models\deepspeech-0.7.4-models.pbmm");
                voice.PipeTo(recognizer);
                recognizer.Do(m =>
                {
                    if (m.IsFinal)
                    {
                        Console.WriteLine(m.Text);
                    }
                });
                recognizer.Write("recognizer", store);


                pipeline.RunAsync();
                Console.WriteLine("Press Enter to Exit...");
                Console.ReadLine();
            }

        }
    }
}
