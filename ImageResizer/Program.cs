﻿using System;
using ImageMagick; // https://github.com/dlemstra/Magick.NET
using System.Drawing;
using System.IO;

// Need to use NuGet for Magick.NET-Q16-x86

/*
    TODO: Multiple files w/wildcards; subdirectories 

 **/
namespace ImageResizer
{
    class Program
    {
        private static bool sVerboseMode = false;
        private static bool sPrintDimensionsOnly = false;
        private static bool sSubdirectories = false;
        static void Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "/?" || args[0] == "-h")
            {
                PrintHelp("");
                return;
            }
            // gather arguments
            string infile = GetArg("-infile:", args);
            string outfile = GetArg("-outfile:", args);
            
            Size outSize = GetTargetDimensions(GetArg("-w:", args), GetArg("-h:", args));
            bool smallerOnly = false;
            if (GetArg("-smalleron", args).Length > 0)
                smallerOnly = true;

            if( infile.Length == 0)
            {
                PrintHelp("Error: Need to specify file"); return;
            }
            if ( (outSize.Width == 0 && outSize.Height == 0) && !sPrintDimensionsOnly )
            {
                PrintHelp("Error: Need to specify size"); return;
            }
            if (HasPattern(infile))
            {
                SearchOption so = SearchOption.TopDirectoryOnly;
                if (sSubdirectories)
                    so = SearchOption.AllDirectories;

                string[] files = Directory.GetFiles(".", infile, so);
                if (files.Length == 0)
                {
                    Console.WriteLine("No Files matching the pattern '{0}' have been found", infile);
                    return;
                }
                foreach(string file in files)
                {
                    try {  
                        ResizeImage(file, file, outSize.Width, outSize.Height, smallerOnly);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error resizing '{0}'. Exception:{1}", file, e.Message);
                    }
                }
            }
            else
            {
                if (!File.Exists(infile))
                {
                    PrintHelp(String.Format("File: '{0}' does not exist.", infile)); return;
                }
                if (outfile.Length == 0)
                    outfile = infile;
                try
                {
                    ResizeImage(infile, outfile, outSize.Width, outSize.Height, smallerOnly);
                }
                catch(Exception e)
                {
                    Console.WriteLine("Error resizing '{0}'. Exception:{1}", infile, e.Message);
                }
            }
        }

        static void ResizeImage(string infile, string outfileName, int width, int height, bool smallerOnly)
        {
            if( (height == 0 && width == 0) && !sPrintDimensionsOnly )
            {
                Console.WriteLine("Cannot resize to 0x0; returning...");
                return;
            }
            using (var collection = new MagickImageCollection(infile))
            {
                // This will remove the optimization and change the image to how it looks at that point
                // during the animation. More info here: http://www.imagemagick.org/Usage/anim_basics/#coalesce
                collection.Coalesce();

                // Resize each image in the collection to a width of 200. When zero is specified for the height
                // the height will be calculated with the aspect ratio.
                foreach (var image in collection)
                {
                    if (sPrintDimensionsOnly)
                    {
                        Console.WriteLine("Image: {0} size:{1}x{2}", infile, image.Width, image.Height);
                        return;
                    }
                    
                    if (width == 0 || height == 0) // width was not specified, set width keeping the ratio
                    {
                        double ratio = (1.0 * image.Width) / image.Height;
                        if (height == 0)
                            height = (int)(width / ratio);
                        if (width == 0)
                            width = (int)(height * ratio);
                    }

                    if (smallerOnly && (image.Width <= width || image.Height <= height))
                    {
                        VerboseMessage(infile+ "> Operation would not decrease image size, not resizing");
                        return;
                    }
                    FileInfo fi = new FileInfo(outfileName);
                    if (fi.IsReadOnly)
                    {
                        Console.WriteLine("ERROR: Unable to write to '{0}' File is 'ReadOnly'. \n", outfileName);
                        return;
                    }
                    image.Resize(width, height);
                }
                VerboseMessage(string.Format("Resizing {0} to {1}x{2}", infile, width, height));
                // Save the result
                try
                {
                    collection.Write(outfileName);
                }
                catch(Exception e)
                {
                    Console.WriteLine("ERROR: Unable to write to '{0}'  \n{1}", outfileName, e.Message);
                }
            }
        }


        /// <summary>
        /// Ok, we're only really checking for the '*' and '?' operators
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        static bool HasPattern(string input)
        {
            if (input.IndexOf('*') > -1 || input.IndexOf('?') > -1)
                return true;
            return false;
        }

        private static void VerboseMessage(string msg)
        {
            if( sVerboseMode) Console.WriteLine(msg);
        }

        private static Size GetTargetDimensions(string w, string h)
        {
            Size retVal = new Size(0, 0);
            int width = 0;
            int height = 0;
            Int32.TryParse(w, out width);
            Int32.TryParse(h, out height);
            retVal.Width = width;
            retVal.Height = height;
            return retVal;
        }

        private static string GetArg(string prefix, string[] args)
        {
            string retVal = "";
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-verbose")
                    sVerboseMode = true;
                if (args[i] == "-dimension")
                    sPrintDimensionsOnly = true;
                if (args[i] == "-s")
                    sSubdirectories = true;
                if (args[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || (prefix == "-infile:" && !args[i].StartsWith("-")))
                {
                    retVal = args[i].Replace(prefix, "");
                    break;
                }
            }
            return retVal;
        }

        private static void PrintHelp(string optionalExtraMessage)
        {
            string message = optionalExtraMessage + "\n" +
@"Usage:
    ImageResizer -infile:<inFileName> -outfile:<outFileName> -w:<width> -h:<height>
  or 
    ImageResizer <inFileName> -w:<width> -h:<height>
  or 
    ImageResizer <FileNamePattern> -w:<width> -h:<height>

Example: (Save a smaller version of an image)
    ImageResizer -infile:Luke.TGA -outfile:SmallLuke.TGA -w:128 -h:128
Example: (Resize an image, if one dimension is omitted aspect ratio will be maintained)
    ImageResizer -infile:Luke.TGA -w:128 

More options:
-smallerOnly    Only resize if it results in a smaller size; don't resize if it would make the image larger.
-verbose        Generate more messaging
-dimension      Only print dimensions of target image, do not resize.
-s              Process through sub directories 

REMARKS <FileNamePattern>
Wildcard specifier	Matches
* (asterisk)	Zero or more characters in that position.
? (question mark)	Zero or one character in that position.
";
            Console.WriteLine(message);
        }

    }
}
