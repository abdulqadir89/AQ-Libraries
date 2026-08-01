import { Node, mergeAttributes } from '@tiptap/core';

export interface AudioEmbedOptions {
  HTMLAttributes: Record<string, unknown>;
}

declare module '@tiptap/core' {
  interface Commands<ReturnType> {
    audioEmbed: {
      setAudioEmbed: (options: { url: string }) => ReturnType;
    };
  }
}

export const AudioEmbed = Node.create<AudioEmbedOptions>({
  name: 'audioEmbed',
  group: 'block',
  atom: true,
  draggable: true,

  addOptions() {
    return { HTMLAttributes: {} };
  },

  addAttributes() {
    return {
      src: { default: null },
    };
  },

  parseHTML() {
    return [
      {
        tag: 'audio',
        getAttrs: (element) => {
          if (typeof element === 'string') return false;
          const source = element.querySelector('source');
          const src = source?.getAttribute('src') ?? element.getAttribute('src');
          return src ? { src } : false;
        },
      },
    ];
  },

  renderHTML({ HTMLAttributes, node }) {
    const src = node.attrs.src as string | null;
    return [
      'audio',
      mergeAttributes(this.options.HTMLAttributes, HTMLAttributes, { controls: 'true' }),
      ['source', { src: src ?? '' }],
    ];
  },

  addCommands() {
    return {
      setAudioEmbed:
        (options: { url: string }) =>
        ({ commands }) => {
          if (!/^https?:\/\//i.test(options.url)) return false;
          return commands.insertContent({
            type: this.name,
            attrs: { src: options.url },
          });
        },
    };
  },
});
