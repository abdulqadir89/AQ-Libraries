import type { ReactNode } from 'react';

export interface CommentThreadItem {
  id: string;
  /** Id of the comment this one answers; null or undefined for a top-level comment. */
  parentId?: string | null;
  text: string;
  authorName: string;
  /** ISO 8601 with offset. */
  createdAt: string;
  editedAt?: string | null;
  isDeleted?: boolean;
  /** Only meaningful on a top-level comment. */
  isResolved?: boolean;
  resolvedByName?: string | null;
  upVotes?: number;
  downVotes?: number;
  /** 1 up, -1 down, 0 or undefined for none. */
  myVote?: number | null;
  /** Whether the current user may edit or delete this comment. */
  canEdit?: boolean;
  /** Shown above the text of a top-level comment, e.g. the quoted text and page of a highlight. */
  context?: ReactNode;
}
