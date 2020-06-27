using System;
using ImageMagick; // https://github.com/dlemstra/Magick.NET
using System.Drawing;
using System.IO;

// Need to use NuGet for Magick.NET-Q16-x86

namespace ImageResizer
{
    class Program
    {
        private static bool sVerboseMode = false; 

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
            if ( outSize.Width == 0 && outSize.Height == 0)
            {
                PrintHelp("Error: Need to specify size"); return;
            }
            if ( !File.Exists(infile))
            {
                PrintHelp(String.Format("File: '{0}' does not exist.", infile)); return;
            }
            if (outfile.Length == 0)
                outfile = infile;
            ResizeImage(infile, outfile, outSize.Width, outSize.Height, smallerOnly);
        }

        static void ResizeImage(string infile, string outfileName, int width, int height, bool smallerOnly)
        {
            if (width == 0)
                width = height;
            if (height == 0)
                height = width;
            if( height == 0 && width == 0 )
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
                    if (smallerOnly && (image.Width <= width || image.Height <= height))
                    {
                        VerboseMessage("Operation would not decrease image size, not resizing");
                        return;
                    }
                    image.Resize(width, height);
                }

                VerboseMessage(string.Format("Resizing {0} to {1}x{2}", infile, width, height));
                // Save the result
                collection.Write(outfileName);
            }
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
                if(args[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || (prefix == "-infile:" && !args[i].StartsWith("-")))
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

Example: (Save a smaller version of an image)
    ImageResizer -infile:Luke.TGA -outfile:SmallLuke.TGA -w:128 -h:128
Example: (Resize an image, if one dimension is omitted the given dimension will be used for both)
    ImageResizer -infile:Luke.TGA -w:128 

More options:
-smallerOnly    Only resize if it results in a smaller size; don't resize if it would make the image larger.
-verbose        Generate more messaging
";
            Console.WriteLine(message);
        }

    }
}
