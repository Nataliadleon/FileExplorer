
using FileExplorer.Models;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace FileExplorer.Helpers;

/// <summary>
/// Extrae metadata EXIF de imágenes, incluyendo coordenadas GPS.
/// Requiere el paquete NuGet MetadataExtractor.
/// </summary>
public static class ExifHelper
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".tif", ".tiff", ".heic", ".png", ".webp" };

    /// <summary>
    /// Rellena los campos EXIF del FileSystemItem si el archivo es una imagen compatible.
    /// </summary>
    public static void PopulateExif(FileSystemItem item)
    {
        if (item.IsDirectory) return;
        if (!SupportedExtensions.Contains(item.Extension)) return;
        if (!File.Exists(item.FullPath)) return;

        try
        {
            var directories = ImageMetadataReader.ReadMetadata(item.FullPath);

            // ─── IFD0 (cámara, dimensiones, color) ───────────────────────────
            var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            if (ifd0 != null)
            {
                item.CameraMake = ifd0.GetDescription(ExifDirectoryBase.TagMake);
                item.CameraModel = ifd0.GetDescription(ExifDirectoryBase.TagModel);
                item.ColorSpace = ifd0.GetDescription(ExifDirectoryBase.TagColorSpace);
            }

            // ─── SubIFD (configuración de captura) ────────────────────────────
            var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (subIfd != null)
            {
                item.DateTaken = subIfd.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);
                item.ExposureTime = subIfd.GetDescription(ExifDirectoryBase.TagExposureTime);
                item.FNumber = subIfd.GetDescription(ExifDirectoryBase.TagFNumber);
                item.IsoSpeed = subIfd.GetDescription(ExifDirectoryBase.TagIsoEquivalent);
                item.Flash = subIfd.GetDescription(ExifDirectoryBase.TagFlash);

                if (subIfd.TryGetInt32(ExifDirectoryBase.TagExifImageWidth, out int w))
                    item.ImageWidth = w;
                if (subIfd.TryGetInt32(ExifDirectoryBase.TagExifImageHeight, out int h))
                    item.ImageHeight = h;
            }

            // ─── GPS ──────────────────────────────────────────────────────────
            var gps = directories.OfType<GpsDirectory>().FirstOrDefault();
            if (gps != null)
            {
                var location = gps.GetGeoLocation();
                if (gps.TryGetDouble(GpsDirectory.TagLatitude, out double lat) &&
                       gps.TryGetDouble(GpsDirectory.TagLongitude, out double lon))
                {
                    item.GpsLatitude = lat;
                    item.GpsLongitude = lon;
                }

                if (gps.TryGetDouble(GpsDirectory.TagAltitude, out double alt))
                    item.GpsAltitude = alt;
            }
        }
        catch { /* Ignorar archivos sin EXIF o dañados */ }
    }
}