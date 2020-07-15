

namespace TBD.Psi.DeepSpeech.Framework
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Speech;

    class Program
    {
        static void Main(string[] args)
        {
            using (var pipeline = Pipeline.Create())
            {
                var store = Store.Create(pipeline, "audioTest", @"C:\Data\Stores");
                var audioInput = new AudioCapture(pipeline, new AudioCaptureConfiguration());
                audioInput.Write("audio", store);

                // Create a VAD & pipe audio to it
                var vad = new SystemVoiceActivityDetector(pipeline);
                audioInput.PipeTo(vad);

                // join the audio and VAD together
                var voice = audioInput.Join(vad);

                // pipe to the recognizer
                var recognizer = new DeepSpeechRecognizer(pipeline);
                voice.PipeTo(recognizer);
                recognizer.Write("recognizer", store);


                pipeline.RunAsync();
                Console.WriteLine("Press Enter to Exit...");
                Console.ReadLine();

            }
        }
    }
}
