using DissectAPI.Dissect;
using DissectAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace DissectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class dissectController : Controller
    {
        [HttpPost("analyze")]
        [RequestSizeLimit(200_000_000)]
        public async Task<IActionResult> UploadFiles([FromForm] List<IFormFile> files)
        {
            // Check if files were uploaded
            if (!files.Any())
            {
                return BadRequest("No files were uploaded.");
            }

            // Check if all files have the .rec extension
            if (!files.All(f => Path.GetExtension(f.FileName).Equals(".rec", StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest("All files must have a .rec extension.");
            }

            // Check if any files have a length of 0
            if (files.Any(x => x.Length <= 0))
            {
                return BadRequest("All files must have a length greater than 0.");
            }

            // Check if files count is greater than 20
            if (files.Count > 20)
            {
                return BadRequest("The maximum number of files that can be uploaded is 20.");
            }

            // Check if any of the Files is bigger than 30mb
            if (files.Any(x => x.Length > 30000000))
            {
                return BadRequest("All files must have a size less than 30mb.");
            }

            long size = files.Sum(f => f.Length);
            var folderName = Path.Combine("UploadedFiles", $"{Guid.NewGuid()}");
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), folderName);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            Debug.WriteLine($"Match Path: {folderPath}");


            foreach (var formFile in files)
            {
                if (formFile.Length > 0)
                {
                    Debug.WriteLine($"Adding File with name: {formFile.FileName}");
                    var filePath = Path.Combine(folderPath, formFile.FileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await formFile.CopyToAsync(stream);
                    }
                }
            }

            Debug.WriteLine($"Files in Folder: {Directory.GetFiles(folderPath).Count()}");

            Debug.WriteLine($"Total Size: {size} bytes");


            Debug.WriteLine("Starting Dissect Process");

            // Run the dissect.exe process with the folder path as argument
            var replay = await DissectHelper.GetReplayAsync(folderPath);

            if (replay == null)
            {
                return BadRequest("Failed to analyze the replay files.");
            }

            DissectHelper.CleanUpFolder(folderPath);

            //Build the response
            var response = new CSVResponse();
            response.CSV = DissectHelper.GetDissectCSV(replay);
            response.ColumnTitles = DissectHelper.GetDissectColumnTitles();

            //Metadata
            string title = replay.Title;
            string rounds = replay.Rounds.Count.ToString();
            string teamA = replay.Rounds.FirstOrDefault()?.TeamA ?? "";
            string teamB = replay.Rounds.FirstOrDefault()?.TeamB ?? "";
            string finalScore = replay.Rounds.LastOrDefault()?.RoundScoreTeamA + ":" + replay.Rounds.LastOrDefault()?.RoundScoreTeamB;


            response.MetaData = new Dictionary<string, string>
            {
                { "Title", title },
                { "Rounds", rounds },
                { "TeamA", teamA },
                { "TeamB", teamB },
                { "FinalScore", finalScore }
            };

            return Ok(response);
        }
    }
}