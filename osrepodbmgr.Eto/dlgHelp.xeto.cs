//
//  Author:
//    Natalia Portillo claunia@claunia.com
//
//  Copyright (c) 2017, © Claunia.com
//
//  All rights reserved.
//
//  Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in
//       the documentation and/or other materials provided with the distribution.
//     * Neither the name of the [ORGANIZATION] nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
//
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
//  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
//  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
//  A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
//  CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
//  EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
//  PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
//  PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
//  LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//  NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//  SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//

using System;
using Eto.Forms;
using Eto.Serialization.Xaml;

namespace osrepodbmgr.Eto
{
    public class dlgHelp : Dialog
    {
        #region XAML UI elements
        #pragma warning disable 0649
        TextArea txtHelp;
        #pragma warning restore 0649
        #endregion XAML UI elements

        public dlgHelp()
        {
            XamlReader.Load(this);

            txtHelp.Text =
                "This is the naming scheme and folder organization conventions for the Operating System Repository.\n\n"                                          +
                "The basic layout is as follows:\n\n"                                                                                                             +
                "<Developer>/<Product>/<Version>/<Language>/<Architecture>/oem/for <Machine>/<[format]_update/upgrade/files/source/netinstall/description>.zip\n" +
                "All fields should contain only 7-bit ASCII.\n\n"                                                                                                 +
                "<Developer>\n"                                                                                                                                   + "-----------\n" +
                "This is the main developer of the operating system or associated software. e.g. Microsoft\n\n"                                                   +
                "<Product>\n"                                                                                                                                     +
                "---------\n"                                                                                                                                     +
                "This is the name of the operating system or associated software. e.g. Windows NT\n\n"                                                            +
                "<Version>\n"                                                                                                                                     +
                "---------\n"                                                                                                                                     +
                "This is the version of the operating system or associated software. e.g. 6.00.6000.16386\n"                                                      +
                "Service pack and release markers should be appended. e.g. 6.10.7601.16385 (RTM)\n"                                                               +
                "Build can be specified by appending \"build\". e.g. 10.2.7 build 6S80\n"                                                                         +
                "And combined. e.g. 10.5 build 9A581 (Server)\n"                                                                                                  +
                "Version with same version number but different build date should have it appended. Date format should be YYYYmm[dd]. e.g. 10 201009\n\n"         +
                "<Language>\n"                                                                                                                                    +
                "----------\n"                                                                                                                                    + "This specifies the language localization and translation:\n" +
                "xxx: Language variation, e.g. German = deu\n"                                                                                                    +
                "xxx_yy: Country specific language variation. e.g. Swiss German = deu_ch\n"                                                                       +
                "multi: The only known variation of the product that contains more than a language\n"                                                             +
                "xxx,xxx,xxx and xxx_yy,xxx_yy,xxx_yy: The variation contains more than a single language\n"                                                      +
                "Where xxx is the ISO-639-2/T language code and yy is the ISO-3166-1 alpha-2 country code.\n"                                                     +
                "https://en.wikipedia.org/wiki/List_of_ISO_639-1_codes\n"                                                                                         +
                "https://en.wikipedia.org/wiki/ISO_3166-1_alpha-2\n"                                                                                              +
                "If the product has ever been only released in one language variation, being it English (like CP/M) or multilanguage "                            +
                "(like Mac OS X and Linux), this field should be omitted.\n\n"                                                                                    +
                "<Architecture>\n"                                                                                                                                +
                "--------------\n"                                                                                                                                +
                "The processor architecture the product is compiled for.\n"                                                                                       +
                "Omitted if it has only ever been compiled for a single one, contains all supported ones in same files, "                                         +
                "or it is source code containing support for several of them.\n"                                                                                  +
                "Exact one depends on the product (that is, the one that the product uses to identify itself, should be used). Examples:\n"                       +
                "* x86, i86, i386, i486, i586, i686, ia32: Intel Architecture 32 aka 8086 aka iAPX86\n"                                                           +
                "* x64, amd64, x86_64: AMD64 aka x86_64 aka EM64T\n"                                                                                              +
                "* ia64: Intel Architecture 64 aka Itanium\n"                                                                                                     +
                "* sparc: SPARC\n"                                                                                                                                +
                "* sun4u, sun4m, sun1, sun2, sun3: Specific Sun architectures that specify not only the processor architecture but the whole system one.\n"       +
                "* 68k, ppc, fat: For products that run under Mac OS indicate they require a Macintosh, a Power Macintosh, or can run on both, respectively.\n"   +
                "* rpi, rpi2, beaglebone, bananapi: Specific whole systems that share a processor architecture but require a completely different variant.\n\n"   +
                "oem\n"                                                                                                                                           +
                "---\n"                                                                                                                                           +
                "Present if the variant is OEM. Omitted otherwise.\n\n"                                                                                           + "for <Machine>\n" +
                "-------------\n"                                                                                                                                 +
                "Present if the variant requires a specific computer to run.\n"                                                                                   +
                "Useful for computer restoration variants.\n"                                                                                                     +
                "e.g. for Power Mac 5200\n"                                                                                                                       +
                "e.g. for Tandy 1000\n\n"                                                                                                                         +
                "<[format]/update/upgrade/files/source/netinstall/description>.zip\n"                                                                             +
                "-----------------------------------------------------------------\n"                                                                             +
                "This is the file containing the product itself.\n"                                                                                               +
                "It should be compressed using ZIP with certain parameters (see below).\n"                                                                        +
                "Several of them can be combined separated with underscores.\n"                                                                                   +
                "Naming as following:\n"                                                                                                                          +
                "* [format]: If the variation is encoded in a disk image format that's neither a simple dump of sectors (.iso/.dsk/.xdf) "                        +
                "or a BinCue/BinToc (.bin+.cue/.bin+.toc) format should be substituted to a descriptive name for the format. e.g.: [Nero], [CloneCD]\n"           +
                "* update: Should be used when the product requires and updates a previous point release or build to the new one. "                               +
                "Product version should be the updated, not the required, one. e.g.: 1.3 updates to 1.3.1 or 2.0 updates to 2.5\n"                                +
                "* upgrade: Should be used when the product requires and updates a previous version to the new one. Product version should be the updated, "      +
                "not the required, one. e.g.: 2.0 updates to 3.0 or MS-DOS updates to Windows 95.\n"                                                              +
                "* files: Should be used when the contents of the product disks are dumped as is (copied from the media) or it contains already installed "       +
                "files.\n"                                                                                                                                        +
                "* source: Should be used when it contains the source code for the product.\n"                                                                    +
                "* netinstall: Similar to files except that the files are designed to be put in a network share for remote installation of the product.\n"        +
                "* description: Free form description or product part number if it is known.\n\n"                                                                 +
                "Compression\n"                                                                                                                                   +
                "-----------\n"                                                                                                                                   +
                "The product should be compressed using ZIP with Deflate algorithm and UTF-8 headers. Zip64 extensions may be used. "                             +
                "UNIX extensions MUST be used for products that require them (e.g. it contains softlinks).\n"                                                     +
                "In the doubt, use Info-ZIP with following parameters:\n"                                                                                         +
                "zip -9ry -dd archive.zip .\n"                                                                                                                    +
                "If the product requires Acorn, BeOS or OS/2 extended attributes it should be compressed using the corresponding Info-ZIP version under "         +
                "that operating system so the required extension is used.\n"                                                                                      +
                "DO NOT recompress archives in an operating system which zip product doesn't support all already present headers.\n"                              +
                "DO NOT use TorrentZip. It discards all headers as well as date stamps.\n"                                                                        +
                "DO NOT use Mac OS headers. For conserving FinderInfo and Resource Fork see below.\n\n"                                                           +
                "FinderInfo and Resource Fork\n"                                                                                                                  +
                "----------------------------\n"                                                                                                                  +
                "FinderInfo and Resource Fork should be stored as Mac OS X AppleDouble format: file and ._file\n"                                                 +
                "This format is understand by all Mac OS X versions under any filesystem or a CIFS/SMB network share.\n"                                          +
                "Also mkisofs recognizes it and is able to create an HFS partition with them correctly set.\n"                                                    +
                "Other formats should be converted to this one.\n\n"                                                                                              +
                "Metadata\n"                                                                                                                                      + "--------\n" +
                "Each archive should be accompanied with a JSON metadata file using the CICM Metadata format. "                                                   +
                "Name for metadata sidecar should be same as the archive changing the extension to .json.\n"                                                      +
                "If the archive can be modified (doesn't contain ZIP headers you would lose) the metadata should be put inside the archive as a file "            +
                "named metadata.json.\n\n"                                                                                                                        +
                "Recovery\n"                                                                                                                                      + "--------\n" +
                "Disks fail, bit rot happens, so every archive as well as the metadata file should have a PAR2 recovery set created.\n"                           +
                "Example command line (with 5% redundancy):\n"                                                                                                    +
                "par2 c -r5 archive.par2 archive.zip archive.json\n\n"                                                                                            +
                "Result\n"                                                                                                                                        + "------\n" +
                "In the end you get something like this:\n"                                                                                                       +
                "Apple/Mac OS/9.1/eng/for iMac (Early 2001) v1.1/archive.json\n"                                                                                  +
                "Apple/Mac OS/9.1/eng/for iMac (Early 2001) v1.1/archive.par2\n"                                                                                  +
                "Apple/Mac OS/9.1/eng/for iMac (Early 2001) v1.1/archive.vol000+01.par2\n"                                                                        +
                "Apple/Mac OS/9.1/eng/for iMac (Early 2001) v1.1/archive.vol001+02.par2\n"                                                                        +
                "Apple/Mac OS/9.1/eng/for iMac (Early 2001) v1.1/archive.vol003+04.par2\n"                                                                        +
                "Apple/Mac OS/9.1/eng/for iMac (Early 2001) v1.1/archive.vol007+08.par2\n"                                                                        +
                "Apple/Mac OS/9.1/eng/for iMac (Early 2001) v1.1/archive.vol015+16.par2\n"                                                                        +
                "Apple/Mac OS/9.1/eng/for iMac (Early 2001) v1.1/archive.vol031+32.par2\n"                                                                        +
                "Apple/Mac OS/9.1/eng/for iMac (Early 2001) v1.1/archive.vol063+37.par2\n"                                                                        +
                "Apple/Mac OS/9.1/eng/for iMac (Early 2001) v1.1/archive.zip\n"                                                                                   +
                "QNX/QNX/20090229/source.json\n"                                                                                                                  +
                "QNX/QNX/20090229/source.par2\n"                                                                                                                  +
                "QNX/QNX/20090229/source.vol000+01.par2\n"                                                                                                        +
                "QNX/QNX/20090229/source.vol001+02.par2\n"                                                                                                        +
                "QNX/QNX/20090229/source.vol003+04.par2\n"                                                                                                        +
                "QNX/QNX/20090229/source.vol007+08.par2\n"                                                                                                        +
                "QNX/QNX/20090229/source.vol015+16.par2\n"                                                                                                        +
                "QNX/QNX/20090229/source.vol031+32.par2\n"                                                                                                        +
                "QNX/QNX/20090229/source.vol063+37.par2\n"                                                                                                        +
                "QNX/QNX/20090229/source.zip";
        }

        protected void OnBtnOKClicked(object sender, EventArgs e)
        {
            Close();
        }
    }
}