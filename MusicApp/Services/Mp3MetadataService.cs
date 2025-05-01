using MusicApp.Shared.Models;
using MusicApp.Interfaces;
using File = TagLib.File;

namespace MusicApp.Services
{
    public class Mp3MetadataService : IMp3MetadataService
    {
        private readonly string _tempDirectory;

        public Mp3MetadataService()
        {
            // Create a dedicated temp directory that we know exists and is writable
            _tempDirectory = Path.Combine(Path.GetTempPath(), "MusicAppTemp");
            if (!Directory.Exists(_tempDirectory))
            {
                Directory.CreateDirectory(_tempDirectory);
            }
        }

        public async Task<Mp3Metadata> ReadMetadataAsync(IFormFile file)
        {
            // Create a unique temporary file path
            string tempFilePath = Path.Combine(_tempDirectory, Path.GetRandomFileName() + ".mp3");
            
            try
            {
                // Save the uploaded file to the temp location
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Use TagLib to read the metadata from the temp file
                using (var tagLibFile = File.Create(tempFilePath))
                {
                    return new Mp3Metadata
                    {
                        Title = tagLibFile.Tag.Title,
                        Artist = tagLibFile.Tag.FirstPerformer,
                        Album = tagLibFile.Tag.Album,
                        Year = tagLibFile.Tag.Year.ToString(),
                        Genre = tagLibFile.Tag.FirstGenre,
                        Comment = tagLibFile.Tag.Comment,
                        TrackNumber = tagLibFile.Tag.Track.ToString()
                    };
                }
            }
            finally
            {
                // Clean up
                if (System.IO.File.Exists(tempFilePath))
                {
                    try
                    {
                        System.IO.File.Delete(tempFilePath);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }

        public async Task<Mp3FileResult> UpdateMetadataAsync(IFormFile file, Mp3Metadata metadata)
        {
            // Create unique temporary file paths
            string tempInputPath = Path.Combine(_tempDirectory, Path.GetRandomFileName() + ".mp3");
            string tempOutputPath = Path.Combine(_tempDirectory, Path.GetRandomFileName() + ".mp3");
            
            try
            {
                // Save the uploaded file to the temp location
                using (var fileStream = new FileStream(tempInputPath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Make a copy for the modified version
                System.IO.File.Copy(tempInputPath, tempOutputPath, true);

                // Update the metadata in the output file
                using (var tagLibFile = File.Create(tempOutputPath))
                {
                    tagLibFile.Tag.Title = metadata.Title;
                    tagLibFile.Tag.Performers = string.IsNullOrEmpty(metadata.Artist) ? new string[0] : new[] { metadata.Artist };
                    tagLibFile.Tag.Album = metadata.Album;
                    
                    if (uint.TryParse(metadata.Year, out uint year))
                        tagLibFile.Tag.Year = year;
                    
                    tagLibFile.Tag.Genres = string.IsNullOrEmpty(metadata.Genre) ? new string[0] : new[] { metadata.Genre };
                    tagLibFile.Tag.Comment = metadata.Comment;
                    
                    if (uint.TryParse(metadata.TrackNumber, out uint track))
                        tagLibFile.Tag.Track = track;
                    
                    // Save changes to the file
                    tagLibFile.Save();
                }

                // Read the modified file
                byte[] updatedFileBytes = await System.IO.File.ReadAllBytesAsync(tempOutputPath);

                // Generate filename
                string newFileName = string.IsNullOrEmpty(metadata.Title) ? 
                    $"modified_{file.FileName}" : 
                    $"{metadata.Title}.mp3";

                return new Mp3FileResult
                {
                    FileContent = updatedFileBytes,
                    FileName = newFileName
                };
            }
            finally
            {
                // Clean up
                try
                {
                    if (System.IO.File.Exists(tempInputPath))
                        System.IO.File.Delete(tempInputPath);
                        
                    if (System.IO.File.Exists(tempOutputPath))
                        System.IO.File.Delete(tempOutputPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}