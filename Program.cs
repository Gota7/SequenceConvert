using GotaSequenceLib;
using GotaSoundIO.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SequenceConvert {

    /// <summary>
    /// Main program.
    /// </summary>
    class Program {

        /// <summary>
        /// Main entrypoint.
        /// </summary>
        /// <param name="args">Arguments.</param>
        static void Main(string[] args) {

            //Help page.
            if (args.Length < 1) {
                Console.WriteLine("Usage: sequenceConvert.exe input (flags) (output)");
                Console.WriteLine("\t-exportLabels (for bin files only, no SSEQ). Exports data offsets to a text file");
                Console.WriteLine("\t-version (for BFSEQ or BCSEQ only). Example: -version 2.1.0");
                Console.WriteLine("\t-endian big/little (for BFSEQ only). Example: -endian big");
                Console.WriteLine("\tSequence Convert is c2020 Gota7.");
                return;
            }

            //Parameters.
            XVersion version = null;
            ByteOrder? byteOrder = null;
            bool exportLabels = false;

            //Input and output.
            string input = args.First();
            string output = null;        

            //Get parameters.
            int argPtr = 1;
            while (argPtr < args.Length) {
                string arg = args[argPtr];
                if (arg.ToLower().Equals("-exportlabels")) {
                    exportLabels = true;
                    argPtr++;
                } else if (arg.ToLower().Equals("-version")) {
                    string[] ver = args[argPtr + 1].Split('.');
                    version = new XVersion() { Major = byte.Parse(ver[0]), Minor = byte.Parse(ver[1]), Revision = byte.Parse(ver[2]) };
                    argPtr += 2;
                } else if (arg.ToLower().Equals("-endian")) {
                    byteOrder = ByteOrder.BigEndian;
                    if (args[argPtr + 1].ToLower().Equals("little")) { byteOrder = ByteOrder.LittleEndian; }
                    argPtr += 2;
                } else {
                    output = arg;
                    argPtr++;
                }
            }

            //Fix output string.
            if (output == null) {

                //Hacks.
                string it = input.ToLower();
                string iz = Path.GetFileNameWithoutExtension(it);
                if (it.EndsWith(".smft")) { output = iz + ".sseq"; }
                else if (it.EndsWith(".sseq")) { output = iz + ".smft"; }

                //Default.
                if (it.Contains(".b")) {
                    output = input.Replace(".b", ".");
                } else {
                    output = input.Replace(".", ".b");
                }

            }

            //Get formats.
            FormatType inFormat = GetFormat(input.ToLower());
            FormatType outFormat = GetFormat(output.ToLower());

            //Fix parameters.
            if (version is null) { version = new XVersion() { Major = 1 }; };
            if (byteOrder == null) { byteOrder = ByteOrder.BigEndian; if (output.ToLower().EndsWith(".bcseq")) { byteOrder = ByteOrder.LittleEndian; } }

            //Run conversion.
            if (inFormat != outFormat || inFormat == FormatType.BXSEQ) {

                //Input and output.
                SequenceFile inFile = null;
                SequenceFile outFile = new BXSEQ();
                bool outputText = false;

                //Input is binary.
                if (inFormat != FormatType.Text) {
                    switch (inFormat) {
                        case FormatType.SSEQ:
                            inFile = Activator.CreateInstance<SSEQ>();
                            break;
                        case FormatType.BRSEQ:
                            inFile = Activator.CreateInstance<BRSEQ>();
                            break;
                        case FormatType.BXSEQ:
                            inFile = Activator.CreateInstance<BXSEQ>();
                            break;
                    }
                    inFile.Read(input);
                }

                //Input is text.
                else {
                    switch (outFormat) {
                        case FormatType.SSEQ:
                            inFile = Activator.CreateInstance<SSEQ>();
                            break;
                        case FormatType.BRSEQ:
                            inFile = Activator.CreateInstance<BRSEQ>();
                            break;
                        case FormatType.BXSEQ:
                            inFile = Activator.CreateInstance<BXSEQ>();
                            break;
                    }
                    inFile.FromText(File.ReadAllLines(input).ToList());
                }

                //Output is binary.
                if (outFormat != FormatType.Text) {

                    //Get type.
                    switch (outFormat) {
                        case FormatType.SSEQ:
                            outFile = Activator.CreateInstance<SSEQ>();
                            break;
                        case FormatType.BRSEQ:
                            outFile = Activator.CreateInstance<BRSEQ>();
                            break;
                        case FormatType.BXSEQ:
                            outFile = Activator.CreateInstance<BXSEQ>();
                            break;
                    }

                }

                //Output is text.
                else {
                    outputText = true;
                }

                //Text output.
                if (outputText) {
                    inFile.Name = Path.GetFileName(output);
                    File.WriteAllLines(output, inFile.ToText());
                }

                //Binary output.
                else {

                    //BXSEQ.
                    if (outFormat == FormatType.BXSEQ) {
                        (outFile as BXSEQ).FType = output.ToLower().EndsWith(".bfseq");
                        outFile.Version = version;
                        outFile.ByteOrder = byteOrder.Value;
                    }

                    //Copy data.
                    outFile.CopyFromOther(inFile);

                    //Write.
                    outFile.WriteCommandData();
                    outFile.Write(output);

                    //Labels.
                    exportLabels &= outFormat != FormatType.SSEQ;
                    if (exportLabels) {
                        List<string> labels = new List<string>();
                        foreach (var l in outFile.Labels) {
                            labels.Add(l.Key + ": " + l.Value);
                        }
                        File.WriteAllLines(Path.GetFileNameWithoutExtension(output) + "Labels.txt", labels.ToArray());
                    }

                }

            }

            //Same type.
            else {
                File.WriteAllBytes(output, File.ReadAllBytes(input));
            }

        }

        /// <summary>
        /// Return the format to convert to.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <returns>Format to convert to.</returns>
        private static FormatType GetFormat(string filePath) {
            FormatType ret = FormatType.Text;
            if (filePath.EndsWith(".sseq")) {
                ret = FormatType.SSEQ;
            } else if (filePath.EndsWith(".brseq")) {
                ret = FormatType.BRSEQ;
            } else if (filePath.EndsWith(".bcseq")) {
                ret = FormatType.BXSEQ;
            } else if (filePath.EndsWith(".bfseq")) {
                ret = FormatType.BXSEQ;
            }
            return ret;
        }

        /// <summary>
        /// Format type.
        /// </summary>
        private enum FormatType { 
            Text,
            SSEQ,
            BRSEQ,
            BXSEQ
        }

    }

}
