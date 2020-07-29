﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AudiobookDb.Database;
using AudiobooksToGo.Dto;
using AudiobookUtil;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AudioBooksToGo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SeriesController : ControllerBase
    {
        private BookCommands _bookCommands;

        public SeriesController()
        {
            this._bookCommands = new BookCommands(SystemVariables.Instance.BookDb);
        }

        //GET: api/Series
        public List<SeriesDto> Get()
        {
            var _series = _bookCommands.GetAllSeries();
            var _result = new List<SeriesDto>();

            foreach (var series in _series)
            {
                var _seriesDto = new SeriesDto();
                _seriesDto.SeriesName = series.SeriesName;
                _seriesDto.SeriesDetailsLink = $"{SystemVariables.Instance.Protocol}://{Request.Host}/api/Series/{series.SeriesId}/Details";
                _result.Add(_seriesDto);
            }
            return _result;
        }

        //GET: api/Series/5
        [HttpGet("{id}", Name = "GetSeries")]
        public SeriesDto Get(string id)
        {
            var _series = _bookCommands.GetSeries(id);
            var _seriesDto = new SeriesDto();
            _seriesDto.SeriesName = _series.SeriesName;
            _seriesDto.SeriesDetailsLink = $"{SystemVariables.Instance.Protocol}://{Request.Host}/api/Series/{_series.SeriesId}/Details";
            return _seriesDto;
        }

        // GET: api/author/5/details
        [HttpGet("{id}/Details", Name = "GetSeriesDetails")]
        public SeriesDetailsDto GetDetails(string id)
        {
            var _seriesDetailsDto = new SeriesDetailsDto();

            var _series = this._bookCommands.GetSeries(id);
            var _seriesBooks = this._bookCommands.GetSeriesBooks(id);
            var _authors = new List<AuthorLinkDto>();
            var _books = new List<BookSeriesLinkDto>();

            foreach (var b in _seriesBooks)
            { 
                var _author = this._bookCommands.GetAuthorsByBookId(b.BookId);
                _authors.AddRange(_author.Select(a => new AuthorLinkDto() { AuthorName = a.AuthorName, AuthorLink = $"{SystemVariables.Instance.Protocol}://{Request.Host}/api/Author/{a.AuthorId}" }).ToList());
                var _book = this._bookCommands.GetBook(b.BookId);
                var _bookTitle = _book.GoodReadsTitle != null ? _book.GoodReadsTitle : _book.BookTitle;
                _books.Add(new BookSeriesLinkDto() { BookTitle = _bookTitle, BookId = _book.BookId, SeriesVolume = b.SeriesVolume, BookLink = $"{SystemVariables.Instance.Protocol}://{Request.Host}/api/Book/{b.BookId}", BookDetailsLink = $"{SystemVariables.Instance.Protocol}://{Request.Host}/api/Book/{b.BookId}/Details", BookCoverLink = $"{SystemVariables.Instance.Protocol}://{Request.Host}/api/Image/{b.BookId}" }); 
            }

            _seriesDetailsDto.SeriesName = _series.SeriesName;
            _seriesDetailsDto.SeriesDetailsHtml = _series.SeriesDescription;
            _seriesDetailsDto.SeriesDetailsText = HtmlHelper.HtmlToPlainText(_series.SeriesDescription);
            _seriesDetailsDto.AuthorLinks = _authors.Distinct().ToList();
            _seriesDetailsDto.BookSeriesLinks = _books;

            return _seriesDetailsDto;
        }
    }
}
