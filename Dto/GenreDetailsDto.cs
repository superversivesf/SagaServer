using System.Collections.Generic;

namespace AudiobooksToGo.Dto
{
    public class GenreDetailsDto
    { 
        public string GenreId { get; set; }
        public string GenreName { get; set; }
        public List<BookLinkDto> GenreBooks { get; set; }
        public List<AuthorLinkDto> GenreAuthors { get; set; }
    }
}
