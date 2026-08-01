import { Node, mergeAttributes } from '@tiptap/core';

const YOUTUBE_ID_PATTERNS = [
  /[?&]v=([A-Za-z0-9_-]{11})/,
  /youtu\.be\/([A-Za-z0-9_-]{11})/,
  /embed\/([A-Za-z0-9_-]{11})/,
];

export function extractYouTubeId(url: string): string | null {
  for (const pattern of YOUTUBE_ID_PATTERNS) {
    const match = url.match(pattern);
    if (match) return match[1];
  }
  return null;
}

export interface VideoEmbedOptions {
  HTMLAttributes: Record<string, unknown>;
}

declare module '@tiptap/core' {
  interface Commands<ReturnType> {
    videoEmbed: {
      setVideoEmbed: (options: { url: string }) => ReturnType;
    };
  }
}

export const VideoEmbed = Node.create<VideoEmbedOptions>({
  name: 'videoEmbed',
  group: 'block',
  atom: true,
  draggable: true,

  addOptions() {
    return { HTMLAttributes: {} };
  },

  addAttributes() {
    return {
      videoId: { default: null },
    };
  },

  parseHTML() {
    return [
      {
        tag: 'iframe[src*="youtube-nocookie.com/embed/"]',
        getAttrs: (element) => {
          if (typeof element === 'string') return false;
          const src = element.getAttribute('src') ?? '';
          const match = src.match(/embed\/([A-Za-z0-9_-]{11})/);
          return match ? { videoId: match[1] } : false;
        },
      },
    ];
  },

  renderHTML({ HTMLAttributes, node }) {
    const videoId = node.attrs.videoId as string | null;
    return [
      'div',
      { class: 'rme-video-embed' },
      [
        'iframe',
        mergeAttributes(this.options.HTMLAttributes, HTMLAttributes, {
          src: videoId ? `https://www.youtube-nocookie.com/embed/${videoId}` : '',
          allow: 'accelerometer;autoplay;clipboard-write;encrypted-media;gyroscope;picture-in-picture',
          allowfullscreen: 'true',
          loading: 'lazy',
          frameborder: '0',
        }),
      ],
    ];
  },

  addCommands() {
    return {
      setVideoEmbed:
        (options: { url: string }) =>
        ({ commands }) => {
          const videoId = extractYouTubeId(options.url);
          if (!videoId) return false;
          return commands.insertContent({
            type: this.name,
            attrs: { videoId },
          });
        },
    };
  },
});
