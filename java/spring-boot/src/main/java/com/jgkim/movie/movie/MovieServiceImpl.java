package com.jgkim.movie.movie;

import io.opentelemetry.instrumentation.annotations.WithSpan;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Component;

import java.util.List;

@Component
@RequiredArgsConstructor
public class MovieServiceImpl implements MovieService {
    private final MovieRepository movieRepository;

    /**
     * 영화 조회
     *
     * @param movieId 영화 ID
     * @return 영화 정보
     */
    @Override
    @WithSpan
    public Movie findMovie(Long movieId) {
        return movieRepository.findById(movieId);
    }

    /**
     * 영화 등록
     *
     * @param movie 영화 정보
     */
    @Override
    @WithSpan
    public void registerMovie(Movie movie) {
        movieRepository.save(movie);
    }

    /**
     * 영화 삭제
     *
     * @param movieId 영화 ID
     */
    @Override
    @WithSpan
    public void removeMovie(Long movieId) {
        movieRepository.delete(movieId);
    }

    /**
     * 영화 정보 수정
     *
     * @param movie 영화 정보
     */
    @Override
    @WithSpan
    public void modifyMovie(Movie movie) {
        movieRepository.update(movie);
    }

    @Override
    @WithSpan
    public List<Movie> findAll() {
        return movieRepository.findAll();
    }
}
