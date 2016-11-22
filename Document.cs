using Nest;
using System;
using System.Collections.Generic;
using System.Text;

namespace FutureOfAttachments
{
    public class Document
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public string Content { get; set; }
        public Attachment Attachment { get; set; }
    }
}
