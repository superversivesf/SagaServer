using System;

namespace AudiobooksToGo.Dto
{
    public class AuthorLinkDto : IEquatable<AuthorLinkDto>
    {
        public string AuthorLink { get; set; }
        public string AuthorName { get; set; }

        bool IEquatable<AuthorLinkDto>.Equals(AuthorLinkDto other)
        {
            var _result = this.AuthorLink.Equals(other.AuthorLink) && this.AuthorName.Equals(other.AuthorName);
            return _result;
        }

        public override int GetHashCode()
        {
            int hashProductName = AuthorName == null ? 0 : AuthorName.GetHashCode();
            int hashProductLink = AuthorLink == null ? 0 : AuthorLink.GetHashCode();

            //Calculate the hash code for the product.
            return hashProductName ^ hashProductLink;
        }
    }
}