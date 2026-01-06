import { useQuery } from '@tanstack/react-query';
import { api } from '../api/client';

export function useBricks() {
  return useQuery({
    queryKey: ['bricks'],
    queryFn: api.getBricks,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

export function useBehaviors() {
  return useQuery({
    queryKey: ['behaviors'],
    queryFn: api.getBehaviors,
    staleTime: 5 * 60 * 1000,
  });
}

export function useAgents() {
  return useQuery({
    queryKey: ['agents'],
    queryFn: api.getAgents,
    staleTime: 5 * 60 * 1000,
  });
}

