import { useInfiniteQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { feedAPI } from '../../services/api';
import type { Post } from '../../types';

export const usePosts = () => {
  const queryClient = useQueryClient();

  const {
    data,
    isLoading,
    error,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
    refetch,
  } = useInfiniteQuery({
    queryKey: ['posts'],
    queryFn: async ({ pageParam = 0 }) => {
      return await feedAPI.getPosts({ skip: pageParam, take: 10 });
    },
    getNextPageParam: (lastPage, allPages) => {
      if (lastPage.length < 10) return undefined;
      return allPages.length * 10;
    },
    initialPageParam: 0,
    staleTime: 1000 * 60 * 2,
  });

  const posts = data?.pages.flatMap((page) => page) ?? [];

  const createPostMutation = useMutation({
    mutationFn: feedAPI.createPost,
    onSuccess: (newPost) => {
      queryClient.setQueryData<Post[]>(['posts'], (old) => {
        return old ? [newPost, ...old] : [newPost];
      });
      queryClient.invalidateQueries({ queryKey: ['posts'] });
    },
  });

  const deletePostMutation = useMutation({
    mutationFn: feedAPI.deletePost,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['posts'] });
    },
  });

  return {
    posts,
    isLoading,
    error,
    refetch,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
    createPost: createPostMutation.mutateAsync,
    deletePost: deletePostMutation.mutateAsync,
  };
};

