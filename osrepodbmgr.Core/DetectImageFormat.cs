// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : DetectImageFormat.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Main program loop.
//
// --[ Description ] ----------------------------------------------------------
//
//     Detects disc image format.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2016 Natalia Portillo
// ****************************************************************************/

using System;
using System.Linq;
using DiscImageChef.DiscImages;
using DiscImageChef.Filters;

namespace osrepodbmgr.Core
{
    public static class ImageFormat
    {
        public static IMediaImage Detect(IFilter imageFilter)
        {
            try
            {
                PluginBase plugins = new PluginBase();

                IMediaImage imageFormat = null;

                // Check all but RAW plugin
                foreach(IMediaImage imageplugin in
                    plugins.ImagePluginsList.Values.Where(p => p.Id != new Guid("12345678-AAAA-BBBB-CCCC-123456789000"))
                )
                    try
                    {
                        if(!imageplugin.Identify(imageFilter)) continue;

                        imageFormat = imageplugin;
                        break;
                    }
                    #pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                    catch { }
                    #pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body

                // Check only RAW plugin
                if(imageFormat != null) return imageFormat;

                foreach(IMediaImage imageplugin in
                    plugins.ImagePluginsList.Values.Where(p => p.Id == new Guid("12345678-AAAA-BBBB-CCCC-123456789000"))
                )
                    try
                    {
                        if(!imageplugin.Identify(imageFilter)) continue;

                        imageFormat = imageplugin;
                        break;
                    }
                    #pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                    catch { }
                    #pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body

                // Still not recognized

                return imageFormat;
            }
            catch { return null; }
        }
    }
}