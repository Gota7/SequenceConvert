# SequenceConvert
Converter for DS, Wii, 3ds, Wii U, and 3ds sequences. MIDI converting is planned in the future.

## Download
[Here](https://github.com/Gota7/SequenceConvert/raw/master/Download/SequenceConvert.exe)

## Usage
SequenceConvert.exe input (flags) (output)
Default will convert the file to its text counter-part and back.

### Flags
* -exportLabels (for bin files only, no SSEQ). Exports data offsets to a text file
* -version (for BFSEQ or BCSEQ only). Example: -version 2.1.0
* -endian big/little (for BFSEQ only). Example: -endian big

## Dependencies
This requires my Sound IO and Sequence libraries which do the heavy lifting.
[GotaSoundIO](https://github.com/Gota7/GotaSoundIO)
[GotaSequenceLib](https://github.com/Gota7/GotaSequenceLib)

## Credits
c2020 Gota7