import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)

const config: Config = {
  title: 'Myelin - UnambitiousFx',
  tagline: 'SSRF-safe outbound HTTP for .NET',
  favicon: 'img/favicon.ico',

  headTags: [
    {
      tagName: 'link',
      attributes: {
        rel: 'icon',
        type: 'image/svg+xml',
        href: '/img/favicon.svg',
      },
    },
    {
      tagName: 'link',
      attributes: {
        rel: 'apple-touch-icon',
        href: '/img/apple-touch-icon.png',
      },
    },
  ],

  // Future flags, see https://docusaurus.io/docs/api/docusaurus-config#future
  future: {
    v4: true, // Improve compatibility with the upcoming Docusaurus v4
  },

  url: 'https://myelin.unambitiousfx.com',
  baseUrl: '/',

  // GitHub pages deployment config.
  organizationName: 'UnambitiousFx',
  projectName: 'Myelin',

  plugins: [
    [
      'docusaurus-plugin-llms',
      {
        generateLLMsTxt: true,
        generateLLMsFullTxt: true,
        excludeImports: true,
        removeDuplicateHeadings: true,
        includeOrder: ['index.mdx', 'getting-started.mdx', 'address-policy.mdx', 'safe-http-client.mdx', 'http-client-factory.mdx'],
        includeUnmatchedLast: true,
      },
    ],
  ],

  onBrokenLinks: 'throw',

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          editUrl: 'https://github.com/UnambitiousFx/Myelin/',
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    image: 'img/unambitiousfx-social-card.png',
    colorMode: {
      respectPrefersColorScheme: true,
    },
    navbar: {
      title: 'Myelin',
      logo: {
        alt: 'UnambitiousFx Myelin Logo',
        src: 'img/unambitiousfx-icon-light.svg',
        srcDark: 'img/unambitiousfx-icon-dark.svg',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'tutorialSidebar',
          position: 'left',
          label: 'Docs',
        },
        {
          type: 'doc',
          docId: 'address-policy',
          position: 'left',
          label: 'Address policy',
        },
        {
          type: 'doc',
          docId: 'http-client-factory',
          position: 'left',
          label: 'IHttpClientFactory',
        },
        {
          href: 'https://www.nuget.org/packages/UnambitiousFx.Myelin',
          label: 'NuGet',
          position: 'right',
        },
        {
          href: 'https://github.com/UnambitiousFx/Myelin',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Docs',
          items: [
            {label: 'Getting Started', to: '/docs/getting-started'},
            {label: 'Address policy', to: '/docs/address-policy'},
            {label: 'IHttpClientFactory', to: '/docs/http-client-factory'},
          ],
        },
        {
          title: 'More',
          items: [
            {label: 'GitHub', href: 'https://github.com/UnambitiousFx/Myelin'},
            {label: 'NuGet', href: 'https://www.nuget.org/packages/UnambitiousFx.Myelin'},
            {label: 'OWASP SSRF', href: 'https://cheatsheetseries.owasp.org/cheatsheets/Server_Side_Request_Forgery_Prevention_Cheat_Sheet.html'},
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} UnambitiousFx. Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['csharp'],
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
