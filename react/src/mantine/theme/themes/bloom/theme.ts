import {
    ActionIcon,
    Alert,
    Anchor,
    AppShell,
    Avatar,
    Badge,
    Blockquote,
    Button,
    Card,
    Checkbox,
    Chip,
    Container,
    createTheme,
    Dialog,
    Indicator,
    Mark,
    NavLink,
    Pagination,
    Paper,
    Radio,
    rem,
    SegmentedControl,
    Select,
    Stepper,
    Switch,
    Table,
    ThemeIcon,
    Timeline,
    Tooltip
} from "@mantine/core";
import type { MantineColorsTuple, MantineThemeOverride } from "@mantine/core";

const CONTAINER_SIZES: Record<string, string> = {
    xxs: rem("200px"),
    xs: rem("300px"),
    sm: rem("400px"),
    md: rem("500px"),
    lg: rem("600px"),
    xl: rem("1400px"),
    xxl: rem("1600px"),
};

// Bloom palette: soft pastel coral progression — gentle, playful, aimed at younger students
const bloomColors: MantineColorsTuple = [
    "#fffaf5",  // 0 paper (lightest)
    "#ffe8e2",  // 1 accent-soft
    "#ffd0c5",  // 2
    "#ffb2a2",  // 3
    "#ff9c8c",  // 4
    "#ff8b7a",  // 5 bloom accent (primary in light mode)
    "#f0715f",  // 6
    "#e8654f",  // 7
    "#d1543f",  // 8
    "#4a3b3f",  // 9 ink (darkest)
    "#ff8b7a",  // 10 Mantine primary shade alias
];

const blush: MantineColorsTuple = ["#faf6f5", "#f3e9e7", "#e8d3cf", "#dcb9b3", "#c39790", "#9c8890", "#7a6169", "#5c4038", "#4a3b3f", "#2b2230", "#9c8890"];
const stoneColors: MantineColorsTuple = ["#fafaf9", "#f5f5f4", "#e7e5e4", "#d6d3d1", "#a8a29e", "#57534e", "#44403c", "#292524", "#1c1917", "#0c0a09", "#78716C"];
const slateColors: MantineColorsTuple = ["#f8fafc", "#f1f5f9", "#e2e8f0", "#cbd5e1", "#94a3b8", "#475569", "#334155", "#1e293b", "#0f172a", "#020817", "#64748B"];
const grayColors: MantineColorsTuple = ["#f9fafb", "#f3f4f6", "#e5e7eb", "#d1d5db", "#9ca3af", "#4b5563", "#374151", "#1f2937", "#111827", "#030712", "#6B7280"];
const neutralColors: MantineColorsTuple = ["#fafafa", "#f5f5f5", "#e5e5e5", "#d4d4d4", "#a3a3a3", "#525252", "#404040", "#262626", "#171717", "#0a0a0a", "#737373"];
const zincColors: MantineColorsTuple = ["#fafafa", "#f4f4f5", "#e4e4e7", "#d4d4d8", "#a1a1aa", "#52525b", "#3f3f46", "#27272a", "#18181b", "#09090b", "#71717A"];
const redColors: MantineColorsTuple = ["#FEF2F2", "#FEE2E2", "#FECACA", "#FCA5A5", "#F87171", "#DC2626", "#B91C1C", "#991B1B", "#7F1D1D", "#450A0A", "#EF4444"];
const roseColors: MantineColorsTuple = ["#fff1f2", "#ffe4e6", "#fecdd3", "#fda4af", "#fb7185", "#e11d48", "#be123c", "#9f1239", "#881337", "#4c0519", "#F43F5E"];
const orangeColors: MantineColorsTuple = ["#fff7ed", "#ffedd5", "#fed7aa", "#fdba74", "#fb923c", "#f97316", "#ea580c", "#9a3412", "#7c2d12", "#431407", "#F97316"];
const amberColors: MantineColorsTuple = ["#FFFBEB", "#FEF3C7", "#FDE68A", "#FCD34D", "#FBBF24", "#f59e0b", "#D97706", "#92400E", "#78350F", "#451A03", "#F59E0B"];
const yellowColors: MantineColorsTuple = ["#fefce8", "#fef9c3", "#fef08a", "#fde047", "#facc15", "#ca8a04", "#a16207", "#854d0e", "#713f12", "#3f2c06", "#F59E0B"];
const limeColors: MantineColorsTuple = ["#f7fee7", "#ecfccb", "#d9f99d", "#bef264", "#a3e635", "#4d7c0f", "#3f6212", "#365314", "#1a2e05", "#0f1903", "#84CC16"];
const greenColors: MantineColorsTuple = ["#F0FDF4", "#DCFCE7", "#BBF7D0", "#86EFAC", "#4ADE80", "#22c55e", "#16A34A", "#166534", "#14532D", "#052E16", "#10B981"];
const emeraldColors: MantineColorsTuple = ["#ecfdf5", "#d1fae5", "#a7f3d0", "#6ee7b7", "#34d399", "#059669", "#047857", "#065f46", "#064e3b", "#022c22", "#10B981"];
const tealColors: MantineColorsTuple = ["#f0fdfa", "#ccfbf1", "#99f6e4", "#5eead4", "#2dd4bf", "#0d9488", "#0f766e", "#115e59", "#134e4a", "#042f2e", "#14B8A6"];
const cyanColors: MantineColorsTuple = ["#ecfeff", "#cffafe", "#a5f3fc", "#67e8f9", "#22d3ee", "#0891b2", "#0e7490", "#155e75", "#164e63", "#083344", "#06B6D4"];
const skyColors: MantineColorsTuple = ["#f0f9ff", "#e0f2fe", "#bae6fd", "#7dd3fc", "#38bdf8", "#0284c7", "#0369a1", "#075985", "#0c4a6e", "#082f49", "#0EA5E9"];
const blueColors: MantineColorsTuple = ["#eff6ff", "#dbeafe", "#bfdbfe", "#93c5fd", "#60a5fa", "#3b82f6", "#2563eb", "#1e40af", "#1e3a8a", "#172554", "#3B82F6"];
const indigoColors: MantineColorsTuple = ["#eef2ff", "#e0e7ff", "#c7d2fe", "#a5b4fc", "#818cf8", "#4f46e5", "#4338ca", "#3730a3", "#312e81", "#1e1b4b", "#6366F1"];
const violetColors: MantineColorsTuple = ["#f5f3ff", "#ede9fe", "#ddd6fe", "#c4b5fd", "#a78bfa", "#7c3aed", "#6d28d9", "#5b21b6", "#4c1d95", "#1e1b4b", "#8B5CF6"];
const purpleColors: MantineColorsTuple = ["#faf5ff", "#f3e8ff", "#e9d5ff", "#d8b4fe", "#c084fc", "#9333ea", "#7e22ce", "#6b21a8", "#581c87", "#2e1065", "#A855F7"];
const fuchsiaColors: MantineColorsTuple = ["#fdf4ff", "#fae8ff", "#f5d0fe", "#f0abfc", "#e879f9", "#c026d3", "#a21caf", "#86198f", "#701a75", "#4a044e", "#D946EF"];
const pinkColors: MantineColorsTuple = ["#fdf2f8", "#fce7f3", "#fbcfe8", "#f9a8d4", "#f472b6", "#db2777", "#be185d", "#9d174d", "#831843", "#500724", "#EC4899"];

export const shadcnTheme: MantineThemeOverride = createTheme({
    colors: {
        slate: slateColors,
        gray: grayColors,
        zinc: zincColors,
        neutral: neutralColors,
        stone: stoneColors,
        blush: blush,
        bloom: bloomColors,

        red: redColors,
        rose: roseColors,
        orange: orangeColors,
        amber: amberColors,
        yellow: yellowColors,

        lime: limeColors,
        green: greenColors,
        emerald: emeraldColors,

        teal: tealColors,
        cyan: cyanColors,
        sky: skyColors,
        blue: blueColors,

        indigo: indigoColors,
        violet: violetColors,
        purple: purpleColors,
        fuchsia: fuchsiaColors,
        pink: pinkColors,

        primary: bloomColors,
        secondary: blush,
        dark: blush,

        error: redColors as MantineColorsTuple,
        success: greenColors,
        info: blueColors,
        warning: amberColors,
    },
    focusRing: "never",
    scale: 1,
    primaryColor: "primary",
    primaryShade: { "light": 5, "dark": 4 },
    autoContrast: true,
    luminanceThreshold: 0.3,
    fontFamily: "Nunito, -apple-system, sans-serif",
    radius: {
        xs: rem("10px"),
        sm: rem("12px"),
        md: rem("16px"),
        lg: rem("20px"),
        xl: rem("28px"),
    },
    defaultRadius: "sm",
    spacing: {
        "4xs": rem("2px"),
        "3xs": rem("4px"),
        "2xs": rem("8px"),
        xs: rem("10px"),
        sm: rem("12px"),
        md: rem("16px"),
        lg: rem("20px"),
        xl: rem("24px"),
        "2xl": rem("28px"),
        "3xl": rem("32px"),
        "4xl": rem("40px"),
    },
    fontSizes: {
        xs: rem("12px"),
        sm: rem("14px"),
        md: rem("16px"),
        lg: rem("18px"),
        xl: rem("20px"),
        "2xl": rem("24px"),
        "3xl": rem("30px"),
        "4xl": rem("36px"),
        "5xl": rem("48px"),
    },
    lineHeights: {
        xs: rem("18px"),
        sm: rem("20px"),
        md: rem("24px"),
        lg: rem("28px"),
    },
    headings: {
        fontFamily: "Quicksand, Nunito, sans-serif",
        fontWeight: "700",
        sizes: {
            h1: { fontSize: rem("36px"), lineHeight: rem("44px"), fontWeight: "700" },
            h2: { fontSize: rem("30px"), lineHeight: rem("38px"), fontWeight: "700" },
            h3: { fontSize: rem("24px"), lineHeight: rem("32px"), fontWeight: "700" },
            h4: { fontSize: rem("20px"), lineHeight: rem("30px"), fontWeight: "600" },
        },
    },
    shadows: {
        xs: "0 1px 2px rgba(232, 101, 79, 0.05)",
        sm: "0 1px 3px rgba(232, 101, 79, 0.08), 0 1px 2px rgba(232, 101, 79, 0.05)",
        md: "0 4px 14px rgba(232, 101, 79, 0.08), 0 1px 3px rgba(232, 101, 79, 0.05)",
        lg: "0 8px 20px rgba(232, 101, 79, 0.12), 0 3px 6px rgba(232, 101, 79, 0.06)",
        xl: "0 10px 26px rgba(255, 139, 122, 0.24)",
        xxl: "0 20px 44px rgba(255, 139, 122, 0.3)",
    },
    cursorType: "pointer",
    other: {
        style: "shadcn",
    },
    components: {
        Container: Container.extend({
            vars: (_, { size, fluid }) => ({
                root: {
                    "--container-size": fluid
                        ? "100%"
                        : size !== undefined && size in CONTAINER_SIZES
                            ? CONTAINER_SIZES[size]
                            : rem(size),
                },
            }),
        }),
        // Warm sidebar via AppShell Navbar component override — light peach, not dark
        AppShell: AppShell.extend({
            styles: () => ({
                navbar: {
                    backgroundColor: 'var(--els-navbar-bg)',
                    borderColor: 'var(--els-navbar-border)',
                    color: 'var(--els-navbar-text)',
                    '--mantine-color-text': 'var(--els-navbar-text)',
                    '--mantine-color-dimmed': 'var(--els-navbar-text-muted)',
                    '--mantine-color-default-color': 'var(--els-navbar-text)',
                    '--els-navlink-active-bg': 'var(--els-navbar-active)',
                    '--els-navlink-active-color': 'var(--els-navbar-active-color)',
                    '--mantine-color-default': 'rgba(255,255,255,0.5)',
                    '--mantine-color-default-border': 'rgba(92,64,56,0.14)',
                    '--mantine-color-default-hover': 'rgba(255,255,255,0.7)',
                },
            }),
        }),
        Table: Table.extend({
            defaultProps: {
                striped: true,
                highlightOnHover: true,
                withTableBorder: true,
                withColumnBorders: false,
                verticalSpacing: 'xs',
                horizontalSpacing: 'sm',
            },
            styles: () => ({
                thead: {
                    backgroundColor: 'light-dark(var(--mantine-color-bloom-1), var(--mantine-color-secondary-8))',
                },
            }),
        }),
        Checkbox: Checkbox.extend({
            vars: (theme, props) => {
                const colorKey = props.color && Object.keys(theme.colors).includes(props.color) ? props.color : undefined;
                return {
                    root: {
                        "--checkbox-color": colorKey ? `var(--mantine-color-${colorKey}-filled)` : 'var(--mantine-primary-color-filled)',
                        "--checkbox-icon-color": colorKey ? `var(--mantine-color-${colorKey}-contrast)` : 'var(--mantine-primary-color-contrast)',
                    },
                };
            },
        }),
        Chip: Chip.extend({
            vars: (theme, props) => {
                const colorKey = props.color && Object.keys(theme.colors).includes(props.color) ? props.color : undefined;
                const variant = props.variant ?? "filled";
                return {
                    root: {
                        "--chip-bg":
                            variant !== "light"
                                ? colorKey
                                    ? `var(--mantine-color-${colorKey}-filled)`
                                    : "var(--mantine-primary-color-filled)"
                                : undefined,
                        "--chip-color":
                            variant === "filled"
                                ? colorKey
                                    ? `var(--mantine-color-${colorKey}-contrast)`
                                    : "var(--mantine-primary-color-contrast)"
                                : undefined,
                    },
                };
            },
        }),
        Radio: Radio.extend({
            vars: (theme, props) => ({
                root: {
                    "--radio-color": props.color
                        ? Object.keys(theme.colors).includes(props.color)
                            ? `var(--mantine-color-${props.color}-filled)`
                            : props.color
                        : "var(--mantine-primary-color-filled)",
                    "--radio-icon-color": props.color
                        ? Object.keys(theme.colors).includes(props.color)
                            ? `var(--mantine-color-${props.color}-contrast)`
                            : props.color
                        : "var(--mantine-primary-color-contrast)",
                },
            }),
        }),
        SegmentedControl: SegmentedControl.extend({
            vars: (theme, props) => ({
                root: {
                    "--sc-color": props.color
                        ? Object.keys(theme.colors).includes(props.color)
                            ? ["bloom", "blush", "stone", "zinc", "slate", "gray", "neutral"].includes(props.color)
                                ? "var(--mantine-color-body)"
                                : `var(--mantine-color-${props.color}-filled)`
                            : props.color
                        : "var(--mantine-color-default)",
                },
            }),
        }),
        Switch: Switch.extend({
            styles: () => ({
                thumb: {
                    backgroundColor: "var(--mantine-color-default)",
                    borderColor: "var(--mantine-color-default-border)",
                },
                track: {
                    borderColor: "var(--mantine-color-default-border)",
                },
            }),
        }),
        Select: Select.extend({
            defaultProps: {
                checkIconPosition: "right",
            },
        }),
        ActionIcon: ActionIcon.extend({
            vars: (theme, props) => {
                const colorKey = props.color && Object.keys(theme.colors).includes(props.color) ? props.color : undefined;
                const isNeutralColor = colorKey && ["bloom", "blush", "stone", "zinc", "slate", "gray", "neutral"].includes(colorKey);
                const isNeutralPrimaryColor = !colorKey && ["bloom", "blush", "stone", "zinc", "slate", "gray", "neutral"].includes(theme.primaryColor);
                const variant = props.variant ?? "filled";
                return {
                    root: {
                        "--ai-color": (() => {
                            if (variant === "filled") {
                                if (colorKey) return `var(--mantine-color-${colorKey}-contrast)`;
                                return "var(--mantine-primary-color-contrast)";
                            }
                            if (variant === "white") {
                                if (isNeutralColor || isNeutralPrimaryColor) return "var(--mantine-color-black)";
                                return undefined;
                            }
                            return undefined;
                        })(),
                    },
                };
            },
        }),
        Button: Button.extend({
            vars: (theme, props) => {
                const colorKey = props.color && Object.keys(theme.colors).includes(props.color) ? props.color : undefined;
                const isNeutralColor = colorKey && ["bloom", "blush", "stone", "zinc", "slate", "gray", "neutral"].includes(colorKey);
                const isNeutralPrimaryColor = !colorKey && ["bloom", "blush", "stone", "zinc", "slate", "gray", "neutral"].includes(theme.primaryColor);
                const variant = props.variant ?? "filled";
                return {
                    root: {
                        "--button-color": (() => {
                            if (variant === "filled") {
                                if (colorKey) return `var(--mantine-color-${colorKey}-contrast)`;
                                return "var(--mantine-primary-color-contrast)";
                            }
                            if (variant === "white") {
                                if (isNeutralColor || isNeutralPrimaryColor) return "var(--mantine-color-black)";
                                return undefined;
                            }
                            return undefined;
                        })(),
                    },
                };
            },
        }),
        Anchor: Anchor.extend({
            defaultProps: {
                underline: "always",
            },
        }),
        NavLink: NavLink.extend({
            vars: (theme, props) => {
                const colorKey = props.color && Object.keys(theme.colors).includes(props.color) ? props.color : undefined;
                const variant = props.variant ?? "light";
                return {
                    root: {
                        "--nl-color":
                            variant === "filled" ? colorKey ? `var(--mantine-color-${colorKey}-contrast)` : 'var(--mantine-primary-color-contrast)'
                            : variant === "light" && props.active ? "var(--els-navlink-active-color, inherit)"
                            : undefined,
                        "--nl-bg":
                            variant === "light" && props.active ? "var(--els-navlink-active-bg)" : undefined,
                    },
                    children: {},
                };
            },
        }),
        Pagination: Pagination.extend({
            vars: (theme, props) => {
                const colorKey = props.color && Object.keys(theme.colors).includes(props.color) ? props.color : undefined;
                return {
                    root: {
                        "--pagination-active-color": colorKey
                            ? `var(--mantine-color-${colorKey}-contrast)`
                            : "var(--mantine-primary-color-contrast)",
                    },
                };
            },
        }),
        Stepper: Stepper.extend({
            vars: (theme, props) => {
                const colorKey = props.color && Object.keys(theme.colors).includes(props.color) ? props.color : undefined;
                return {
                    root: {
                        "--stepper-icon-color": colorKey
                            ? `var(--mantine-color-${colorKey}-contrast)`
                            : "var(--mantine-primary-color-contrast)",
                    },
                };
            },
        }),
        Alert: Alert.extend({
            vars: (theme, props) => {
                const colorKey = props.color && Object.keys(theme.colors).includes(props.color) ? props.color : undefined;
                const isNeutralColor = colorKey && ["bloom", "blush", "stone", "zinc", "slate", "gray", "neutral"].includes(colorKey);
                const isNeutralPrimaryColor = !colorKey && ["bloom", "blush", "stone", "zinc", "slate", "gray", "neutral"].includes(theme.primaryColor);
                const variant = props.variant ?? "light";
                return {
                    root: {
                        "--alert-color":
                            variant === "filled"
                                ? colorKey
                                    ? `var(--mantine-color-${colorKey}-contrast)`
                                    : "var(--mantine-primary-color-contrast)"
                                : variant === "white"
                                    ? (isNeutralColor || isNeutralPrimaryColor
                                        ? `var(--mantine-color-black)`
                                        : undefined)
                                    : undefined,
                    },
                };
            },
        }),
        Dialog: Dialog.extend({
            defaultProps: {
                withBorder: true,
            },
        }),
        Tooltip: Tooltip.extend({
            vars: () => ({
                tooltip: {
                    "--tooltip-bg": "var(--mantine-color-primary-color-filled)",
                    "--tooltip-color": "var(--mantine-color-primary-color-contrast)",
                },
            }),
        }),
        Avatar: Avatar.extend({
            vars: (theme, props) => {
                const colorKey = props.color && Object.keys(theme.colors).includes(props.color) ? props.color : undefined;
                const isNeutralColor = colorKey && ["bloom", "blush", "stone", "zinc", "slate", "gray", "neutral"].includes(colorKey);
                const isNeutralPrimaryColor = !colorKey && ["bloom", "blush", "stone", "zinc", "slate", "gray", "neutral"].includes(theme.primaryColor);
                const variant = props.variant ?? "light";
                return {
                    root: {
                        "--avatar-bg":
                            variant === "filled"
                                ? colorKey
                                    ? `var(--mantine-color-${colorKey}-filled)`
                                    : "var(--mantine-primary-color-filled)"
                                : variant === "light"
                                    ? colorKey
                                        ? `var(--mantine-color-${colorKey}-light)`
                                        : "var(--mantine-primary-color-light)"
                                    : undefined,
                        "--avatar-color":
                            variant === "filled"
                                ? colorKey
                                    ? `var(--mantine-color-${colorKey}-contrast)`
                                    : "var(--mantine-primary-color-contrast)"
                                : variant === "light"
                                    ? colorKey
                                        ? `var(--mantine-color-${colorKey}-light-color)`
                                        : "var(--mantine-primary-color-light-color)"
                                    : variant === "white"
                                        ? isNeutralColor || isNeutralPrimaryColor
                                            ? `var(--mantine-color-black)`
                                            : colorKey
                                                ? `var(--mantine-color-${colorKey}-outline)`
                                                : "var(--mantine-primary-color-filled)"
                                        : variant === "outline" || variant === "transparent"
                                            ? colorKey
                                                ? `var(--mantine-color-${colorKey}-outline)`
                                                : "var(--mantine-primary-color-filled)"
                                            : undefined,
                        "--avatar-bd":
                            variant === "outline"
                                ? colorKey
                                    ? `1px solid var(--mantine-color-${colorKey}-outline)`
                                    : "1px solid var(--mantine-primary-color-filled)"
                                : undefined,
                    },
                };
            },
        }),
        Badge: Badge.extend({
            vars: (theme, props) => {
                const colorKey = props.color && Object.keys(theme.colors).includes(props.color) ? props.color : undefined;
                const isNeutralColor = colorKey && ["bloom", "blush", "stone", "zinc", "slate", "gray", "neutral"].includes(colorKey);
                const isNeutralPrimaryColor = !colorKey && ["bloom", "blush", "stone", "zinc", "slate", "gray", "neutral"].includes(theme.primaryColor);
                const variant = props.variant ?? "filled";
                return {
                    root: {
                        "--badge-bg": variant === "filled" && colorKey ? `var(--mantine-color-${colorKey}-filled)` : undefined,
                        "--badge-color":
                            variant === "filled"
                                ? (colorKey ? `var(--mantine-color-${colorKey}-contrast)` : 'var(--mantine-primary-color-contrast)')
                                : variant === "white"
                                    ? (isNeutralColor || isNeutralPrimaryColor
                                        ? `var(--mantine-color-black)`
                                        : undefined)
                                    : undefined,
                    },
                };
            },
        }),
        Card: Card.extend({
            defaultProps: {
                p: "xl",
                shadow: "xl",
                withBorder: true,
            },
            styles: () => ({
                root: {
                    backgroundColor: "light-dark(var(--mantine-color-body), var(--mantine-color-default))",
                },
            }),
        }),
        Indicator: Indicator.extend({
            vars: (theme, props) => {
                const colorKey = props.color && Object.keys(theme.colors).includes(props.color) ? props.color : undefined;
                return {
                    root: {
                        "--indicator-text-color": colorKey
                            ? `var(--mantine-color-${colorKey}-contrast)`
                            : "var(--mantine-primary-color-contrast)",
                    },
                };
            },
        }),
        ThemeIcon: ThemeIcon.extend({
            vars: (theme, props) => {
                const colorKey = props.color && Object.keys(theme.colors).includes(props.color) ? props.color : undefined;
                const isNeutralColor = colorKey && ["bloom", "blush", "stone", "zinc", "slate", "gray", "neutral"].includes(colorKey);
                const isNeutralPrimaryColor = !colorKey && ["bloom", "blush", "stone", "zinc", "slate", "gray", "neutral"].includes(theme.primaryColor);
                const variant = props.variant ?? "filled";
                return {
                    root: {
                        "--ti-color": variant === "filled"
                            ? (colorKey
                                ? `var(--mantine-color-${colorKey}-contrast)`
                                : "var(--mantine-primary-color-contrast)")
                            : variant === "white"
                                ? (isNeutralColor || isNeutralPrimaryColor
                                    ? `var(--mantine-color-black)`
                                    : undefined)
                                : undefined,
                    },
                };
            },
        }),
        Timeline: Timeline.extend({
            vars: (theme, props) => {
                const colorKey = props.color && Object.keys(theme.colors).includes(props.color) ? props.color : undefined;
                return {
                    root: {
                        "--tl-icon-color": colorKey ? `var(--mantine-color-${colorKey}-contrast)` : 'var(--mantine-primary-color-contrast)',
                    },
                };
            },
        }),
        Blockquote: Blockquote.extend({
            vars: (theme, props) => {
                const colorKey = props.color && Object.keys(theme.colors).includes(props.color) ? props.color : undefined;
                return {
                    root: {
                        "--bq-bg-dark": colorKey ? `var(--mantine-color-${colorKey}-light)` : 'var(--mantine-primary-color-light)',
                        "--bq-bg-light": colorKey ? `var(--mantine-color-${colorKey}-light)` : 'var(--mantine-primary-color-light)',
                    },
                };
            },
        }),
        Mark: Mark.extend({
            vars: (theme, props) => {
                const colorKey = props.color && Object.keys(theme.colors).includes(props.color) ? props.color : 'yellow';
                const isNeutralColor = colorKey && ["bloom", "blush", "stone", "zinc", "slate", "gray", "neutral"].includes(colorKey);
                return {
                    root: {
                        "--mark-bg-light": `var(--mantine-color-${colorKey}-${isNeutralColor ? '3' : 'filled-hover'})`,
                        "--mark-bg-dark": `var(--mantine-color-${colorKey}-filled)`,
                    },
                };
            },
        }),
        Paper: Paper.extend({
            defaultProps: {
                shadow: "xl",
            },
            styles: () => ({
                root: {
                    backgroundColor: "light-dark(var(--mantine-color-body), var(--mantine-color-default))",
                },
            }),
        }),
    },
});
