//dotnet run
//http://localhost:5198/swagger
//http://localhost:8080/phpmyadmin
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lab2.Controllers;

[ApiController]
[Route("[controller]")]
public class MoviesController : ControllerBase
{
    [HttpPost("UploadMovieCsv")]
    public string PostUploadMovieCSV(IFormFile inputFile)
    {
        var strm = inputFile.OpenReadStream();
        byte[] buffer = new byte[inputFile.Length];
        strm.Read(buffer, 0, (int)inputFile.Length);
        string fileContent = System.Text.Encoding.Default.GetString(buffer);
        strm.Close();

        MoviesContext dbContext = new MoviesContext();

        bool skip_header = true;

        foreach (string line in fileContent.Split('\n'))
        {
            if (skip_header)
            {
                skip_header = false;
                continue;
            }
            var tokens = line.Split(",");
            if (tokens.Length != 3)
                continue;
            string MovieID = tokens[0];
            // If the Movie is already in the database, skip it
            if (dbContext.Movies.Any(e => e.MovieID == int.Parse(MovieID)))
                continue;
            string MovieName = tokens[1];
            string[] Genres = tokens[2].Trim('\r').Split("|");
            List<Genre> movieGenres = new List<Genre>();
            // Collect all the Genres that are not already in the database
            List<Genre> newGenres = new List<Genre>();
            foreach (string genre in Genres)
            {
                Genre g = new Genre();
                g.Name = genre;
                

                // If the Genre already in the list, skip it
                if (movieGenres.Any(e => e.Name == g.Name))
                    continue;
                // Add the Genre to the movieGenres list
                movieGenres.Add(g);
                // If the Genre is not in the database, add it to the list
                if (!dbContext.Genres.Any(e => e.Name == g.Name))
                {
                    newGenres.Add(g);
                }
            }
            // Add the new Genres to the database
            if (newGenres.Count > 0)
            {
                dbContext.Genres.AddRange(newGenres);
                dbContext.SaveChanges();
            }
            Movie m = new Movie();
            m.MovieID = int.Parse(MovieID);
            m.Title = MovieName;
            List<Genre> movieGenresFromDB = new List<Genre>();
            foreach(Genre g in movieGenres)
            {
                movieGenresFromDB.Add(dbContext.Genres.Where(e => g.Name == e.Name).First());
            }
            m.Genres = movieGenresFromDB;
            dbContext.Movies.Add(m);
            dbContext.SaveChanges();
        }
        dbContext.SaveChanges();
        return "OK";
    }

    [HttpGet("GetAllGenres")]
    public IEnumerable<Genre> GetAllGenres()
    {
        MoviesContext dbContext = new MoviesContext();
        return dbContext.Genres.AsEnumerable();
    }

    [HttpGet("GetMoviesByName/{search_phrase}")]
    public IEnumerable<Movie> GetMoviesByName(string search_phrase)
    {
        MoviesContext dbContext = new MoviesContext();
        return dbContext.Movies.Where(e => e.Title.Contains(search_phrase));
    }

    [HttpPost("GetMoviesByGenre")]
    public IEnumerable<Movie> GetMoviesByGenre(string search_phrase)
    {
        MoviesContext dbContext = new MoviesContext();
        return dbContext.Movies.Where(m => m.Genres.Any(p => p.Name.Contains(search_phrase)));
    }

    [HttpPost("UploadRatingCsv")]
    public string PostUploadRatingCSV(IFormFile inputFile)
    {
        // Read the file
        var strm = inputFile.OpenReadStream();
        byte[] buffer = new byte[inputFile.Length];
        strm.Read(buffer, 0, (int)inputFile.Length);
        string fileContent = System.Text.Encoding.Default.GetString(buffer);
        strm.Close();

        MoviesContext dbContext = new MoviesContext();

        bool skip_header = true;
        foreach (string line in fileContent.Split('\n'))
        {
            if (skip_header)
            {
                skip_header = false;
                continue;
            }
            var tokens = line.Split(",");
            if (tokens.Length != 4)
                continue;
            string UserID = tokens[0];
            string MovieID = tokens[1];
            string Rating = tokens[2];

            if (!dbContext.Movies.Any(e => e.MovieID == int.Parse(MovieID)))
                continue;
            if (
                dbContext.Ratings.Any(
                    e =>
                        e.RatingUser.UserID == int.Parse(UserID)
                        && e.RatedMovie.MovieID == int.Parse(MovieID)
                )
            )
                continue;

            if (!dbContext.Users.Any(e => e.UserID == int.Parse(UserID)))
            {
                User u = new User();
                u.UserID = int.Parse(UserID);
                u.Name = "User" + UserID;
                dbContext.Users.Add(u);
                dbContext.SaveChanges();
            }

            Rating r = new Rating();
            r.RatingValue = (int)float.Parse(Rating) % 5 + 1;
            r.RatedMovie = dbContext.Movies.First(e => e.MovieID == int.Parse(MovieID));
            r.RatingUser = dbContext.Users.First(e => e.UserID == int.Parse(UserID));
            dbContext.Ratings.Add(r);
            dbContext.SaveChanges();
        }
        return "OK";
    }

    //--------------------------------------------------------------------------------------------------------//
  
    [HttpPost("GetGenresByMovieId")]
    public string GetGenresByMovieId(int id)
    {
        MoviesContext dbContext = new MoviesContext();
        var genres = dbContext.Genres.Where(g => g.Movies.Any(m => m.MovieID == id));
        string genreNames = string.Join(", ", genres.Select(g => g.Name));
        return genreNames;
    }

    [HttpPost("GetGenresByMovieIdVector")]
    public IEnumerable<string> GetGenresByMovieIdVector(int id)
    {
        using (MoviesContext dbContext = new MoviesContext())
        {
            var genres = dbContext.Genres
                .Where(g => g.Movies.Any(m => m.MovieID == id))
                .Select(g => g.Name)
                .ToList();
            return genres;
        }
    }
    
    [HttpPost("CompareMoviesByGenresVector")]
    public ActionResult<double> CompareMoviesByGenresVector(int movieId1, int movieId2)
    {
        var genres1 = GetGenresByMovieIdVector(movieId1);
        var genres2 = GetGenresByMovieIdVector(movieId2);
        var commonGenres = genres1.Intersect(genres2);
        double dotProduct = commonGenres.Count();
        double norm1 = genres1.Count();
        double norm2 = genres2.Count();
        double cosineSimilarity = dotProduct / Math.Sqrt(norm1 * norm2);
        return cosineSimilarity;
    }

    [HttpPost("GetRelatedMovies")]
    public ActionResult<List<String>> GetRelatedMovies(int movieId)
    {
       using (MoviesContext dbContext = new MoviesContext())
        {
            var targetGenres = GetGenresByMovieIdVector(movieId);
            // Get related movies based on shared genres
            var relatedMovies = dbContext.Movies
            .Where(m => m.Genres.Any(g => targetGenres.Contains(g.Name)))
            .Select(m => m.Title)
            .ToList();

             return relatedMovies;
        }
    }

    [HttpPost("GetSimilarMovies")]
    public ActionResult<IEnumerable<string>> GetSimilarMovies(int movieId, double threshold)
    {
        using (MoviesContext dbContext = new MoviesContext())
        {
            var similarMovies = new List<string>();
            foreach (var movie in dbContext.Movies)
            {
                if (movie.MovieID == movieId)
                {
                    continue;
                }
                var cosineSimilarity = CompareMoviesByGenresVector(movieId, movie.MovieID).Value;
                if (cosineSimilarity >= threshold)
                {
                    similarMovies.Add(movie.Title);
                }
            }
            return similarMovies;
        }
    }

    [HttpPost("GetRatedMoviesByUserId")]
    public ActionResult<List<string>> GetRatedMoviesByUserId(int UserID)
    {
            using (MoviesContext dbContext = new MoviesContext())
        {
            var ratedMovies = dbContext.Ratings
                .Where(r => r.RatingUser.UserID == UserID)
                .Select(r => r.RatedMovie.Title)
                .ToList();

            return ratedMovies;
        }
    }

    [HttpPost("GetRatedMoviesByUserIdSortedByRating")]
    public List<string> GetRatedMoviesByUserIdSortedByRating(int UserID)
    {
        using (MoviesContext dbContext = new MoviesContext())
        {
            var ratedMovies = dbContext.Ratings
                .Where(r => r.RatingUser.UserID == UserID)
                .OrderByDescending(r => r.RatingValue)
                .Select(r => r.RatedMovie.Title)
                .ToList();

            return ratedMovies;
        }
    }


    [HttpPost("GetSimilarMoviesToHighestRatedMovieByUser")]   
    public ActionResult<List<string>> GetSimilarMoviesToHighestRatedMovieByUser(int userID)
    {
        // Get the movie that received the highest rating from the user
        var highestRatedMovie = GetRatedMoviesByUserIdSortedByRating(userID).FirstOrDefault();
        var highestRatedMovieId = GetMoviesByName(highestRatedMovie).FirstOrDefault().MovieID;

        Console.WriteLine($"highestRatedMovie: {highestRatedMovie}");
        Console.WriteLine($"highestRatedMovieId: {highestRatedMovieId}");

        if (highestRatedMovie == null)
        {
            return NotFound();
        }

        // Get similar movies to the highest rated movie
        var similarMovies = GetSimilarMovies(highestRatedMovieId, 0.5).Value.ToList();

        // Filter the list of similar movies based on cosine similarity
        var filteredMovies = new List<string>();
        foreach (var movie in similarMovies)
        {
            var movieId = GetMoviesByName(movie).FirstOrDefault().MovieID;
            var cosineSimilarity = CompareMoviesByGenresVector(highestRatedMovieId, movieId).Value;
            if (cosineSimilarity >= 0.5)
            {
                filteredMovies.Add(movie);
            }
        }

        return filteredMovies;
    }

}
