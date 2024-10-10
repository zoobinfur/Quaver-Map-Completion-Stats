# Quaver-Map-Completion-Stats
A tool to measure how many ranked maps a player has completed in Quaver.<br/>
Simply run the .exe (or build it from the source code) and enter your Quaver player ID.

- Provides an overall completion percentage statisitc and statitistics for different difficulty ranges.
- Gives the names of maps which stop a difficulty range from being 100% complete (hopefully useful for completionists).
- Uses the Quaver API (v2) to retrieve both maps and scores. The API has a ratelimit of 100 requests per minute, which means the program currently takes ~1 hour to complete.
<br/>
<br/>

TODO:
+ Add 7k support.
+ Add difficulty range customisability.
