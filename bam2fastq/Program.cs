using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Bio;
using Bio.IO.PacBio;
using Bio.IO.FastQ;

namespace bam2fastq
{
    class MainClass
    {
        public static void Main (string[] args)
        {
            try {
                PlatformManager.Services.MaxSequenceSize = int.MaxValue;
                PlatformManager.Services.DefaultBufferSize = 4096;
                PlatformManager.Services.Is64BitProcessType = true;

                if (args.Length > 3) {
                    Console.WriteLine ("Too many arguments");
                    DisplayHelp ();
                } else if (args.Length < 2) {
                    Console.WriteLine("Not enough arguments");
                    DisplayHelp();
                } else if (args [0] == "h" || args [0] == "help" || args [0] == "?" || args [0] == "-h") {
                    DisplayHelp ();
                } else {
                    string bam_name = args [0];
                    string threshold = args [1];
                    string output = args [2];
                    if (!File.Exists(bam_name)) {
                        Console.WriteLine ("Can't find file: " + bam_name);
                        return;
                    }
                    double min_rq;
                    bool converted = Double.TryParse(threshold, out min_rq);
                    if (!converted) {
                        Console.WriteLine ("Could not parse minimum threshold from : " + threshold + " expected decimal number in [0,1] interval.");
                        return;
                    }
                    if (min_rq < 0.0 || min_rq > 1.0) {
                        Console.WriteLine ("Minimum RQ value: " + min_rq + " was not in [0,1] interval.");
                        return;
                    }
                    if (File.Exists (output)) {
                        Console.WriteLine ("The output file already exists, please specify a new name or delete the old one.");
                        return;
                    }

                    var fastq = new FastQFormatter();
                    fastq.FormatType = FastQFormatType.Sanger;
                    var os = new FileStream(output, FileMode.CreateNew);
                    // Filter and output
                    PacBioCCSBamReader bamreader = new PacBioCCSBamReader ();
                    int numRead = 0;
                    int numFiltered = 0;
                    foreach(var read in bamreader.Parse(bam_name)) {
                        numRead++;
                        var ccs = read as PacBioCCSRead;
                        if (ccs.ReadQuality > min_rq) {
                            //read.ID = read.ID + "/RQ=" + read.ReadQuality; 
                            fastq.Format(os,read);
                        }
                        else {
                            numFiltered ++;
                        }
                    }
                    os.Close();
                    Console.WriteLine("Parsed " + numRead + " reads and filtered out " + numFiltered + " for RQ < " + min_rq); 
                }
            }
            catch(DllNotFoundException thrown) {
                Console.WriteLine ("Error thrown when attempting to generate the CCS results.");
                Console.WriteLine("A shared library was not found.  To solve this, please add the folder" +
                    " with the downloaded file libMonoPosixHelper" +
                    "to your environmental variables (LD_LIBRARY_PATH and DYLD_LIBRARY_PATH on Mac OS X)."); 
                Console.WriteLine ("Error: " + thrown.Message);
                Console.WriteLine (thrown.StackTrace);

            }
            catch(Exception thrown) {
                Console.WriteLine ("Error thrown when attempting to generate the FASTQ File");
                Console.WriteLine ("Error: " + thrown.Message);
                Console.WriteLine (thrown.StackTrace);
                while (thrown.InnerException != null) {
                    Console.WriteLine ("Inner Exception: " + thrown.InnerException.Message);
                    thrown = thrown.InnerException;
                }
            }
        }

        static void DisplayHelp() {
            Console.WriteLine ("bam2fastq INPUT OUTDIR ");
            Console.WriteLine ("INPUT - the input ccs.bam file");
            Console.WriteLine ("THRESHOLD - [0,1] RQ threshold for output");
            Console.WriteLine ("OUTPUT - A fastq filename to output");
        }          
    }
}
