﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SagaServer.Dto
{
    public class AuthorDetailsDto
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public List<BookLinkDto> BookLinks {get;set;}
        public string HtmlDescription { get; set; }
        public string TextDescription { get; set; }
        public string ImageLink { get; set; }
        public string WebsiteLink { get; set; }
        public string Born { get; set; }
        public string Died { get; set; }
        public string Genre { get; set; }
        public string Influences { get; set; }
        public string Twitter { get; set; }

    }
}
