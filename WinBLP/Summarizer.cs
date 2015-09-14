using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WinBLP
{
    /// <summary>
    /// Summarizer collects and collates the information from the various
    /// log files organized by type of bat and pass duration.
    /// It 'knows' the names or tags for different bat species and is passed
    /// the descriptive comment for each pass along with the pass duration.
    /// The comments are parsed to identify the bat type or types and the
    /// information is collated for an external caller to be able to retrieve
    /// and display.
    /// </summary>
    class Summarizer
    {
        

        public Summarizer() { }

    }
}
