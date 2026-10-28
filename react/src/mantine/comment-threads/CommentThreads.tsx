import { useMemo, useState } from 'react';
import {
  ActionIcon, Badge, Box, Button, Group, Paper, Stack, Text, Textarea, Tooltip,
} from '@mantine/core';
import { modals } from '@mantine/modals';
import { IconArrowBigDown, IconArrowBigUp, IconCheck, IconRotate } from '@tabler/icons-react';
import { DateTimeOffsetDisplay } from '../datetime';
import { useAQLocale } from '../locale';
import type { CommentThreadItem } from './types';

export interface CommentThreadsProps {
  comments: CommentThreadItem[];
  /** A read-only list hides every action, e.g. once a review is closed. */
  readOnly?: boolean;
  canReply?: boolean;
  /** Replies nest no deeper than this: 1 allows replies to top-level comments only. Unset, replies nest without limit. */
  maxDepth?: number;
  canVote?: boolean;
  canResolve?: boolean;
  selectedId?: string | null;
  onSelect?: (id: string) => void;
  /** Adds a top-level comment without an anchor. Omit to hide the composer. */
  onAdd?: (text: string) => Promise<void> | void;
  onReply?: (parentId: string, text: string) => Promise<void> | void;
  onEdit?: (id: string, text: string) => Promise<void> | void;
  onDelete?: (id: string) => Promise<void> | void;
  onResolve?: (id: string) => Promise<void> | void;
  onReopen?: (id: string) => Promise<void> | void;
  /** value 1 up, -1 down, 0 removes the vote. */
  onVote?: (id: string, value: number) => Promise<void> | void;
}

interface Node {
  item: CommentThreadItem;
  children: Node[];
}

function buildTree(items: CommentThreadItem[]): Node[] {
  const nodes = new Map<string, Node>(items.map((item) => [item.id, { item, children: [] }]));
  const roots: Node[] = [];
  for (const node of nodes.values()) {
    const parent = node.item.parentId ? nodes.get(node.item.parentId) : undefined;
    if (parent) parent.children.push(node);
    else roots.push(node);
  }
  return roots;
}

interface ComposerProps {
  placeholder: string;
  submitLabel: string;
  cancelLabel?: string;
  initialValue?: string;
  onSubmit: (text: string) => Promise<void> | void;
  onCancel?: () => void;
}

function Composer({ placeholder, submitLabel, cancelLabel, initialValue = '', onSubmit, onCancel }: ComposerProps) {
  const [value, setValue] = useState(initialValue);
  const [busy, setBusy] = useState(false);

  const submit = async () => {
    const text = value.trim();
    if (!text) return;
    setBusy(true);
    try {
      await onSubmit(text);
      setValue('');
    } finally {
      setBusy(false);
    }
  };

  return (
    <Stack gap="xs">
      <Textarea
        value={value}
        onChange={(e) => setValue(e.currentTarget.value)}
        placeholder={placeholder}
        autosize
        minRows={2}
        maxRows={8}
      />
      <Group gap="xs" justify="flex-end">
        {onCancel && (
          <Button size="xs" variant="default" onClick={onCancel} disabled={busy}>{cancelLabel}</Button>
        )}
        <Button size="xs" onClick={submit} loading={busy} disabled={!value.trim()}>{submitLabel}</Button>
      </Group>
    </Stack>
  );
}

interface CommentNodeProps extends Omit<CommentThreadsProps, 'comments' | 'onAdd'> {
  node: Node;
  depth: number;
}

function CommentNode({ node, depth, ...props }: CommentNodeProps) {
  const { messages } = useAQLocale();
  const t = messages.commentThreads;
  const { item } = node;
  const [replying, setReplying] = useState(false);
  const [editing, setEditing] = useState(false);
  const [collapsed, setCollapsed] = useState(false);
  const isRoot = depth === 0;
  const selected = props.selectedId === item.id;
  // RULE RCT-13
  const interactive = !props.readOnly;

  const confirmDelete = () => modals.openConfirmModal({
    title: t.deleteConfirmTitle,
    children: <Text size="sm">{t.deleteConfirmContent}</Text>,
    labels: { confirm: t.delete, cancel: t.cancel },
    confirmProps: { color: 'red' },
    onConfirm: () => props.onDelete?.(item.id),
  });

  return (
    <Box>
      <Paper
        withBorder
        p="xs"
        radius="sm"
        onClick={() => props.onSelect?.(item.id)}
        style={{
          cursor: props.onSelect ? 'pointer' : undefined,
          borderColor: selected ? 'var(--mantine-primary-color-filled)' : undefined,
        }}
      >
        <Stack gap={4}>
          <Group justify="space-between" gap="xs">
            <Group gap={6}>
              <Text size="sm" fw={600}>{item.authorName}</Text>
              <DateTimeOffsetDisplay value={item.createdAt} textProps={{ size: 'xs', c: 'dimmed' }} />
              {item.editedAt && <Text size="xs" c="dimmed">({t.edited})</Text>}
            </Group>
            {isRoot && item.isResolved && (
              <Tooltip label={item.resolvedByName ? t.resolvedBy(item.resolvedByName) : t.resolved}>
                <Badge size="xs" color="green" variant="light">{t.resolved}</Badge>
              </Tooltip>
            )}
          </Group>

          {isRoot && item.context}

          {item.isDeleted ? (
            <Text size="sm" c="dimmed" fs="italic">{t.deletedComment}</Text>
          ) : editing ? (
            <Composer
              placeholder={t.commentPlaceholder}
              submitLabel={t.save}
              cancelLabel={t.cancel}
              initialValue={item.text}
              onSubmit={async (text) => {
                await props.onEdit?.(item.id, text);
                setEditing(false);
              }}
              onCancel={() => setEditing(false)}
            />
          ) : (
            <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>{item.text}</Text>
          )}

          {!item.isDeleted && !editing && (
            <Group gap={4} onClick={(e) => e.stopPropagation()}>
              <Tooltip label={t.upvote}>
                <ActionIcon
                  size="sm"
                  variant={item.myVote === 1 ? 'filled' : 'subtle'}
                  disabled={!interactive || !props.canVote}
                  onClick={() => props.onVote?.(item.id, item.myVote === 1 ? 0 : 1)}
                  aria-label={t.upvote}
                >
                  <IconArrowBigUp size={14} />
                </ActionIcon>
              </Tooltip>
              <Text size="xs" c="dimmed">{(item.upVotes ?? 0) - (item.downVotes ?? 0)}</Text>
              <Tooltip label={t.downvote}>
                <ActionIcon
                  size="sm"
                  variant={item.myVote === -1 ? 'filled' : 'subtle'}
                  disabled={!interactive || !props.canVote}
                  onClick={() => props.onVote?.(item.id, item.myVote === -1 ? 0 : -1)}
                  aria-label={t.downvote}
                >
                  <IconArrowBigDown size={14} />
                </ActionIcon>
              </Tooltip>

              {interactive && props.canReply && (props.maxDepth === undefined || depth < props.maxDepth) && (
                <Button size="compact-xs" variant="subtle" onClick={() => setReplying((v) => !v)}>{t.reply}</Button>
              )}
              {interactive && item.canEdit && (
                <>
                  <Button size="compact-xs" variant="subtle" onClick={() => setEditing(true)}>{t.edit}</Button>
                  <Button size="compact-xs" variant="subtle" color="red" onClick={confirmDelete}>{t.delete}</Button>
                </>
              )}
              {interactive && isRoot && props.canResolve && (
                item.isResolved ? (
                  <Button
                    size="compact-xs"
                    variant="subtle"
                    leftSection={<IconRotate size={12} />}
                    onClick={() => props.onReopen?.(item.id)}
                  >
                    {t.reopen}
                  </Button>
                ) : (
                  <Button
                    size="compact-xs"
                    variant="subtle"
                    color="green"
                    leftSection={<IconCheck size={12} />}
                    onClick={() => props.onResolve?.(item.id)}
                  >
                    {t.resolve}
                  </Button>
                )
              )}
            </Group>
          )}
        </Stack>
      </Paper>

      {replying && interactive && (
        <Box ml="md" mt={4}>
          <Composer
            placeholder={t.replyPlaceholder}
            submitLabel={t.reply}
            cancelLabel={t.cancel}
            onSubmit={async (text) => {
              await props.onReply?.(item.id, text);
              setReplying(false);
            }}
            onCancel={() => setReplying(false)}
          />
        </Box>
      )}

      {node.children.length > 0 && (
        <Box ml="md" mt={4}>
          <Button size="compact-xs" variant="subtle" onClick={() => setCollapsed((v) => !v)}>
            {collapsed ? t.showReplies(node.children.length) : t.hideReplies}
          </Button>
          {!collapsed && (
            <Stack gap={4} mt={4}>
              {node.children.map((child) => (
                <CommentNode key={child.item.id} node={child} depth={depth + 1} {...props} />
              ))}
            </Stack>
          )}
        </Box>
      )}
    </Box>
  );
}

export function CommentThreads({ comments, onAdd, ...props }: CommentThreadsProps) {
  const { messages } = useAQLocale();
  const t = messages.commentThreads;
  const tree = useMemo(() => buildTree(comments), [comments]);

  return (
    <Stack gap="sm">
      {onAdd && !props.readOnly && (
        <Composer placeholder={t.commentPlaceholder} submitLabel={t.post} onSubmit={onAdd} />
      )}
      {tree.length === 0 ? (
        <Text size="sm" c="dimmed">{t.noComments}</Text>
      ) : (
        tree.map((node) => <CommentNode key={node.item.id} node={node} depth={0} {...props} />)
      )}
    </Stack>
  );
}
