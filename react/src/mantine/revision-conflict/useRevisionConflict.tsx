import { useCallback, useEffect, useRef } from 'react';
import { Button, Stack, Text } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { isPreconditionFailed } from '../../utils/EntityTag';
import { useAQLocale } from '../locale';

/**
 * Returns a handler for a failed save. When the error is a refused stale save (412) it shows a toast with a Reload
 * button (which calls `onReload`) and returns true; otherwise it returns false and the caller handles the error.
 * The user's typed values stay in the form until they choose Reload.
 */
// RULE RCT-19
export function useRevisionConflict(onReload: () => unknown): (error: unknown) => boolean {
  const { messages } = useAQLocale();
  const m = messages.revisionConflict;
  const reload = useRef(onReload);
  useEffect(() => {
    reload.current = onReload;
  });

  return useCallback(
    (error: unknown) => {
      if (!isPreconditionFailed(error)) return false;
      const id = 'aq-revision-conflict';
      notifications.show({
        id,
        title: m.title,
        color: 'red',
        autoClose: false,
        message: (
          <Stack gap="xs" align="flex-start">
            <Text size="sm">{m.message}</Text>
            <Button
              size="compact-sm"
              variant="light"
              color="red"
              onClick={() => {
                notifications.hide(id);
                void reload.current();
              }}
            >
              {m.reload}
            </Button>
          </Stack>
        ),
      });
      return true;
    },
    [m.title, m.message, m.reload],
  );
}
