using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AudiobookDb.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AudioBooksToGo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AudioFileController : ControllerBase
    {
        private BookCommands _bookCommands;

        public AudioFileController()
        {
            this._bookCommands = new BookCommands(SystemVariables.Instance.BookDb);
        }

        // GET: api/AudioFile/5
        [HttpGet("{id}", Name = "GetAudioFile")]
        public IActionResult Get(string id)
        {
            var _audioFile = this._bookCommands.GetAudioFile(id);

            if (_audioFile == null)
                return NotFound();

            var _filePath = Path.Combine(_audioFile.AudioFileFolder, _audioFile.AudioFileName);
            var _fileBytes = System.IO.File.ReadAllBytes(_filePath);
            var _memoryStream = new MemoryStream(_fileBytes);

            return File(_memoryStream, "audio/mpeg", _audioFile.AudioFileName);
        }
    }
}
