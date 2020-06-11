using GotaSoundIO.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SequenceConvert {

    /// <summary>
    /// NDS header.
    /// </summary>
    public class NHeader : FileHeader {

        /// <summary>
        /// Read the header.
        /// </summary>
        /// <param name="r">The reader.</param>
        public override void Read(FileReader r) {
            Magic = new string(r.ReadChars(4));
            r.ByteOrder = ByteOrder.BigEndian;
            r.ByteOrder = ByteOrder = r.ReadUInt16() == 0xFEFF ? ByteOrder.BigEndian : ByteOrder.LittleEndian;
            r.ReadUInt16(); //Version is always constant.
            FileSize = r.ReadUInt32();
            HeaderSize = r.ReadUInt16();
            r.ReadUInt16();
            BlockOffsets = new long[] { 0x10 };
        }

        /// <summary>
        /// Write the header.
        /// </summary>
        /// <param name="w">The writer.</param>
        public override void Write(FileWriter w) {
            HeaderSize = 0x10;
            w.ByteOrder = ByteOrder.LittleEndian;
            w.Write(Magic.ToCharArray());
            w.Write((ushort)0xFEFF);
            w.Write((ushort)0x0100);
            w.Write((uint)FileSize);
            w.Write((ushort)HeaderSize);
            w.Write((ushort)1);
        }

    }

    /// <summary>
    /// A version.
    /// </summary>
    public class RVersion : GotaSoundIO.IO.Version {

        /// <summary>
        /// Create a version from a ushort.
        /// </summary>
        /// <param name="s">The ushort.</param>
        public void FromUShort(ushort s) {
            Major = (byte)((s & 0xFF00) >> 8);
            Minor = (byte)(s & 0xFF);
        }

        /// <summary>
        /// Convert the version to a ushort.
        /// </summary>
        /// <returns>The version as a ushort.</returns>
        public ushort ToUShort() {
            return (ushort)((Major << 8) | Minor);
        }

        /// <summary>
        /// Read the version.
        /// </summary>
        /// <param name="r">The reader.</param>
        public override void Read(FileReader r) {
            FromUShort(r.ReadUInt16());
        }

        /// <summary>
        /// Write the version.
        /// </summary>
        /// <param name="w">The writer.</param>
        public override void Write(FileWriter w) {
            w.Write(ToUShort());
        }

    }

    /// <summary>
    /// A revolution file header.
    /// </summary>
    public class RFileHeader : FileHeader {

        /// <summary>
        /// Read the header.
        /// </summary>
        /// <param name="r">The reader.</param>
        public override void Read(FileReader r) {
            Magic = new string(r.ReadChars(4));
            ByteOrder = r.ByteOrder = ByteOrder.BigEndian;
            if (r.ReadUInt16() == 0xFFFE) {
                ByteOrder = ByteOrder.LittleEndian;
            }
            Version = r.Read<RVersion>();
            FileSize = r.ReadUInt32();
            HeaderSize = r.ReadUInt16();
            ushort numBlocks = r.ReadUInt16();
            BlockOffsets = new long[numBlocks];
            BlockSizes = new long[numBlocks];
            for (int i = 0; i < numBlocks; i++) {
                BlockOffsets[i] = r.ReadUInt32();
                BlockSizes[i] = r.ReadUInt32();
            }
        }

        /// <summary>
        /// Write the header.
        /// </summary>
        /// <param name="w">The writer.</param>
        public override void Write(FileWriter w) {
            w.Write(Magic.ToCharArray());
            w.ByteOrder = ByteOrder;
            w.Write((ushort)0xFEFF);
            w.Write(Version);
            w.Write((uint)FileSize);
            HeaderSize = 0x10 + 8 * BlockOffsets.Length;
            while (HeaderSize % 0x20 != 0) {
                HeaderSize++;
            }
            w.Write((ushort)HeaderSize);
            w.Write((ushort)BlockOffsets.Length);
            for (int i = 0; i < BlockOffsets.Length; i++) {
                w.Write((uint)BlockOffsets[i]);
                w.Write((uint)BlockSizes[i]);
            }
            w.Align(0x20);
        }

    }

    /// <summary>
    /// 3ds, Wii U, and Switch.
    /// </summary>
    public class XVersion : GotaSoundIO.IO.Version {

        /// <summary>
        /// If the version is an F one and not a C one.
        /// </summary>
        public bool FType = true;

        /// <summary>
        /// Read the version.
        /// </summary>
        /// <param name="r">The reader.</param>
        public override void Read(FileReader r) {
            uint version = r.ReadUInt32();
            if (FType) {
                Major = (byte)((version & 0x00FF0000) >> 16);
                Minor = (byte)((version & 0x0000FF00) >> 8);
                Revision = (byte)(version & 0x000000FF);
            } else {
                Major = (byte)((version & 0xFF000000) >> 24);
                Minor = (byte)((version & 0x00FF0000) >> 16);
                Revision = (byte)((version & 0x0000FF00) >> 8);
            }
        }

        /// <summary>
        /// Write the version.
        /// </summary>
        /// <param name="w">The writer.</param>
        public override void Write(FileWriter w) {
            uint ver = 0;
            if (FType) {
                ver |= (uint)(Major << 16);
                ver |= (uint)(Minor << 8);
                ver |= (uint)(Revision << 0);
            } else {
                ver |= (uint)(Major << 24);
                ver |= (uint)(Minor << 16);
                ver |= (uint)(Revision << 8);
            }
            w.Write(ver);
        }

    }

    /// <summary>
    /// 3ds, Wii U, and Switch header.
    /// </summary>
    public class XFileHeader : FileHeader {

        /// <summary>
        /// F type.
        /// </summary>
        public bool FType => Magic.StartsWith("F");

        /// <summary>
        /// Read the header.
        /// </summary>
        /// <param name="r">The reader.</param>
        public override void Read(FileReader r) {
            Magic = new string(r.ReadChars(4));
            r.ByteOrder = ByteOrder = ByteOrder.BigEndian;
            if (r.ReadUInt16() == 0xFFFE) {
                ByteOrder = r.ByteOrder = ByteOrder.LittleEndian;
            }
            HeaderSize = r.ReadUInt16();
            Version = new XVersion();
            (Version as XVersion).FType = FType;
            Version.Read(r);
            FileSize = r.ReadUInt32();
            ushort numBlocks = r.ReadUInt16();
            r.ReadUInt16();
            BlockOffsets = new long[numBlocks];
            BlockSizes = new long[numBlocks];
            BlockTypes = new long[numBlocks];
            for (int i = 0; i < numBlocks; i++) {
                BlockTypes[i] = r.ReadUInt16();
                r.ReadUInt16();
                BlockOffsets[i] = r.ReadUInt32();
                BlockSizes[i] = r.ReadUInt32();
            }
        }

        /// <summary>
        /// Write the header.
        /// </summary>
        /// <param name="w">The writer.</param>
        public override void Write(FileWriter w) {
            w.Write(Magic.ToCharArray());
            w.ByteOrder = ByteOrder;
            w.Write((ushort)0xFEFF);
            w.Write((ushort)HeaderSize);
            (Version as XVersion).FType = FType;
            w.Write(Version);
            w.Write((uint)FileSize);
            w.Write((ushort)BlockOffsets.Length);
            w.Write((ushort)0);
            for (int i = 0; i < BlockOffsets.Length; i++) {
                w.Write((ushort)BlockTypes[i]);
                w.Write((ushort)0);
                w.Write((uint)BlockOffsets[i]);
                w.Write((uint)BlockSizes[i]);
            }
            w.Align(0x20);
        }

    }

    /// <summary>
    /// Reference for 3ds, Wii U, and Switch.
    /// </summary>
    /// <typeparam name="T">Reference type.</typeparam>
    public class XReference<T> : Reference<T> {

        /// <summary>
        /// Null reference is 0.
        /// </summary>
        /// <returns>If the null reference is 0.</returns>
        public override bool NullReferenceIs0() => false;

        /// <summary>
        /// If to set the current offset when jumping.
        /// </summary>
        /// <returns>Set current offset when jumping.</returns>
        public override bool SetCurrentOffsetOnJump() => true;

        /// <summary>
        /// Read the reference.
        /// </summary>
        /// <param name="r">The reader.</param>
        public override void ReadRef(FileReader r) {
            Identifier = r.ReadUInt16();
            r.ReadUInt16();
            Offset = r.ReadInt32();
        }

        /// <summary>
        /// Write the reference.
        /// </summary>
        /// <param name="w">The writer.</param>
        /// <param name="ignoreNullData">If to ignore raw data.</param>
        public override void WriteRef(FileWriter w, bool ignoreNullData = false) {
            if (!ignoreNullData && ((NullReferenceIs0() && Offset == 0) || (!NullReferenceIs0() && Offset == -1))) {
                w.Write((uint)0);
                w.Write(-1);
            } else {
                w.Write((ushort)Identifier);
                w.Write((ushort)0);
                w.Write((int)Offset);
            }
        }

    }

}
