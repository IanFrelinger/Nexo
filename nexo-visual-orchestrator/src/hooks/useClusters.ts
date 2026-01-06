import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api, type Cluster, type ClusterDetail, type CreateClusterRequest } from '../api/client';

export function useClusters(tag?: string, scope?: string) {
  return useQuery({
    queryKey: ['clusters', tag, scope],
    queryFn: () => api.getClusters(tag, scope),
    staleTime: 5 * 60 * 1000,
  });
}

export function useCluster(id: string) {
  return useQuery({
    queryKey: ['cluster', id],
    queryFn: () => api.getCluster(id),
    enabled: !!id,
    staleTime: 5 * 60 * 1000,
  });
}

export function useCreateCluster() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (request: CreateClusterRequest) => api.createCluster(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['clusters'] });
    },
  });
}

