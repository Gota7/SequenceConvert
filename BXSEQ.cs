using GotaSequenceLib;
using GotaSoundIO;
using GotaSoundIO.IO;
using GotaSoundIO.Sound;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace SequenceConvert {
    
    /// <summary>
    /// 3ds, Wii U, Switch.
    /// </summary>
    public class BXSEQ : SequenceFile {

        /// <summary>
        /// F type.
        /// </summary>
        public bool FType = true;

        /// <summary>
        /// If switch.
        /// </summary>
        public bool IsSwitch => FType && ByteOrder == ByteOrder.LittleEndian; 

        /// <summary>
        /// Platform.
        /// </summary>
        /// <returns>Platform.</returns>
        public override SequencePlatform Platform() => IsSwitch ? (SequencePlatform)new NX() : new CtrCafe();

        /// <summary>
        /// Read the file.
        /// </summary>
        /// <param name="r">The reader.</param>
        public override void Read(FileReader r) {

            //Open file.
            FileHeader fileHeader;
            r.OpenFile<XFileHeader>(out fileHeader);
            ByteOrder = fileHeader.ByteOrder;
            Version = fileHeader.Version;
            FType = fileHeader.Magic.StartsWith("F");

            //Data block.
            uint dataSize;
            r.OpenBlock(0, out _, out dataSize);
            var data = r.ReadBytes((int)(dataSize - 8)).ToList();

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

            //Label block.
            r.OpenBlock(1, out _, out _);
            var labels = r.Read<Table<XReference<LabelEntry>>>().Select(x => x.Data);
            Labels = new Dictionary<string, uint>();
            foreach (var l in labels) {
                Labels.Add(l.Str, l.Offset);
            }

        }

        /// <summary>
        /// Write the file.
        /// </summary>
        /// <param name="w">The writer.</param>
        public override void Write(FileWriter w) {

            //Init file.
            w.InitFile<XFileHeader>(FType ? "FSEQ" : "CSEQ", ByteOrder, Version, 2);

            //Data block.
            w.InitBlock("DATA", blockType: 0x5000);
            w.Write(RawData);
            w.Align(0x20);
            w.CloseBlock();

            //Label block.
            w.InitBlock("LABL", blockType: 0x5001);
            w.Write((uint)Labels.Count);
            for (int i = 0; i < Labels.Count; i++) {
                w.InitReference<XReference<object>>("Labl" + i);
            }
            for (int i = 0; i < Labels.Count; i++) {
                w.CloseReference("Labl" + i, 0x5100);
                w.Write(new LabelEntry() { Offset = Labels.Values.ElementAt(i), Str = Labels.Keys.ElementAt(i) });
            }
            w.Align(0x20);
            w.CloseBlock();

            //Close file.
            w.CloseFile();

        }

        /// <summary>
        /// Label entry.
        /// </summary>
        private class LabelEntry : IReadable, IWritable {
            public uint Offset;
            public string Str;
            public void Read(FileReader r) {
                r.ReadUInt32();
                Offset = r.ReadUInt32();
                int strLen = r.ReadInt32();
                Str = new string(r.ReadChars(strLen));
            }
            public void Write(FileWriter w) {
                w.Write((ushort)0x1F00);
                w.Write((ushort)0);
                w.Write(Offset);
                w.Write(Str.Length);
                w.Write(Str.ToCharArray());
                w.Write((byte)0);
                w.Align(0x4);
            }
        }

    }

}
