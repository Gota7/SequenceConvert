using GotaSequenceLib;
using GotaSoundIO;
using GotaSoundIO.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SequenceConvert {
    
    /// <summary>
    /// Wii.
    /// </summary>
    public class BRSEQ : SequenceFile {

        /// <summary>
        /// Sequence platform.
        /// </summary>
        /// <returns>The sequence platform.</returns>
        public override SequencePlatform Platform() => new Revolution();

        /// <summary>
        /// Read the sequence file.
        /// </summary>
        /// <param name="r">The file reader.</param>
        public override void Read(FileReader r) {

            //Open file.
            r.OpenFile<RFileHeader>(out _);

            //Data block.
            uint dataSize;
            r.OpenBlock(0, out _, out dataSize);
            uint off = r.ReadUInt32();
            r.Jump(off - 8);
            var data = r.ReadBytes((int)(dataSize - off)).ToList();

            //Remove padding.
            for (int i = data.Count - 1; i >= 0; i--) {
                if (data[i] == 0) {
                    data.RemoveAt(i);
                } else {
                    break;
                }
            }

            //Set data.
            RawData = data.ToArray();

            //Get labels.
            r.OpenBlock(1, out _, out _);
            Table<uint> labelOffs = r.Read<Table<uint>>();
            Labels = new Dictionary<string, uint>();
            foreach (var l in labelOffs) {
                r.Jump(l);
                uint labelOff = r.ReadUInt32();
                Labels.Add(new string(r.ReadChars((int)r.ReadUInt32())), labelOff);
            }

        }

        /// <summary>
        /// Write the sequence file.
        /// </summary>
        /// <param name="w">The writer.</param>
        public override void Write(FileWriter w) {

            //Init file.
            w.InitFile<RFileHeader>("RSEQ", ByteOrder.BigEndian, new RVersion() { Major = 1, Minor = 0 }, 2);

            //Data block.
            w.InitBlock("DATA");
            w.Write((uint)0xC);
            w.Write(RawData);
            w.Align(0x20, true);
            w.CloseBlock();

            //Label block.
            w.InitBlock("LABL");
            w.Write((uint)Labels.Count);
            for (int i = 0; i < Labels.Count; i++) {
                w.InitOffset("label" + i);
            }
            for (int i = 0; i < Labels.Count; i++) {
                w.CloseOffset("label" + i);
                w.Write(Labels.Values.ElementAt(i));
                w.Write((uint)Labels.Keys.ElementAt(i).Length);
                w.Write(Labels.Keys.ElementAt(i).ToCharArray());
                w.Write((byte)0);
                w.Align(4, true);
            }
            w.Align(0x20, true);
            w.CloseBlock();

            //Close file.
            w.CloseFile();

        }

    }

}
