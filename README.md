Movie Recommended System
========================

This is a simple movie recommendation system based on the hypothesis that movies with similar genres are similar to each other and users with similar movie ratings are similar to each other. The system uses data from the MovieLens dataset and is implemented as a web API using Entity Framework and MySQL (MariaDB).

Installation
------------

To use the system, you will need to install the following:

- Visual Studio Code
- MySQL Server 8.0 or later
- XAMMP
- Swagger

Usage
-----

The system provides the following web methods:

1. **GetGenresByMovieId(int movieId)**: Returns all genres related to a movie with a given id.
2. **GetGenresByMovieIdVector(int movieId)**: Returns a vector of genres related to a movie with a given id.
3. **CompareMoviesByGenresVector(int movieId1, int movieId2)**: Compares two movies and returns a cosine similarity measure based on their genres.
4. **GetRelatedMovies(int movieId)**: Returns a list of movies. Each movie on the return list needs to share at least one genre with the movie given as an input.
5. **GetSimilarMovies(int movieId, double threshold)**: Returns a list of movies. Each movie on the return list needs to be similar to the movie given as an input (the cosine similarity needs to be higher than the threshold).
6. **GetRatedMoviesByUserId(int userId)**: Returns a list of movies rated by a user with a given id.
7. **GetRatedMoviesByUserIdSortedByRating(int userId)**: Returns a list of movies rated by a user with a given id, sorted by the rating.
8. **GetSimilarMoviesToHighestRatedMovieByUser(int userId)**: Returns a list of movies, similar to the movie highest rated by a user with a given id.
9. **GetRecommendedMoviesByUserId(int userId, int size)**: Returns a set of recommendations of a given size for a user with a given id.

Bonus Tasks
-----------

1. **GetRecommendedMoviesByUserSimilarity(int userId, int size)**: Returns recommendations for a user with a given id based on the hypothesis (H2). Instead of comparing movies, compare the users and select movies, which are highly rated by similar users. Remember not to recommend movies, which are already rated by a given user.
2. **GetRecommendedMoviesByCustomScore(int userId, int size)**: Combines the methods described in Tasks 1 and 2. Come up with your own score function for a recommendation.

License
-------

This project is licensed under the Apache 2.0 License. See the `LICENSE` file for details.

