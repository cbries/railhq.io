// @ts-check
// `@type` JSDoc annotations allow editor autocompletion and type checking
// (when paired with `@ts-check`).
// There are various equivalent ways to declare your Docusaurus config.
// See: https://docusaurus.io/docs/api/docusaurus-config

import {themes as prismThemes} from 'prism-react-renderer';

// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)

/** @type {import('@docusaurus/types').Config} */
const config = {
  title: 'railhq.io - Innovative Cloud-Lösung für Modelleisenbahnen',
  tagline: 'Innovative Cloud-Lösung für Modelleisenbahnen – einzigartig in ihrem Funktionsumfang',
  favicon: 'img/favicon.ico',

  url: 'https://railhq.io',
  baseUrl: '/documentation',

  // GitHub pages deployment config.
  // If you aren't using GitHub pages, you don't need these.
  organizationName: '', // Usually your GitHub org/user name.
  projectName: '', // Usually your repo name.

  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',

  // Even if you don't use internationalization, you can use this field to set
  // useful metadata like html lang. For example, if your site is Chinese, you
  // may want to replace "en" with "zh-Hans".
  i18n: {
    defaultLocale: 'de',
    locales: ['de'],
  },

  presets: [
    [
      'classic',
      /** @type {import('@docusaurus/preset-classic').Options} */
      ({
        docs: {
          sidebarPath: './sidebars.js'
        },
        blog: {
          showReadingTime: true,
          feedOptions: {
            type: ['rss', 'atom'],
            xslt: true,
          },
          // Useful options to enforce blogging best practices
          onInlineTags: 'warn',
          onInlineAuthors: 'warn',
          onUntruncatedBlogPosts: 'warn'
        },
        theme: {
          customCss: './src/css/custom.css',
        },
      }),
    ],
  ],

  themeConfig:
    /** @type {import('@docusaurus/preset-classic').ThemeConfig} */
    ({
      // Replace with your project's social card
      image: 'img/logo-social-card.png',
      navbar: {
        title: 'railhq.io',
        logo: {
          alt: 'railhq.io',
          src: 'img/logo-200x200.png',
        },
        items: [
          {
            to: 'https://railhq.io', 
            label: 'Startseite', 
            position: 'left'
          },
          {
            to: 'https://railhq.io/dashboard', 
            label: 'Übersicht', 
            position: 'left'
          },
          {
            type: 'docSidebar',
            sidebarId: 'tutorialSidebar',
            position: 'left',
            label: 'Anleitungen',
          }
          // ,
          // {to: '/blog', label: 'Blog', position: 'left'},
          // {
          //   href: 'https://github.com/facebook/docusaurus',
          //   label: 'GitHub',
          //   position: 'right',
          // },
        ],
      },
      footer: {
        style: 'dark',
        links: [
          {
            title: 'Links',
            items: [
              {
                label: 'About',
                to: 'https://railhq.io/about'
              },
              {
                label: 'Kontakt',
                to: 'https://railhq.io/contact'
              }
            ],
          },
          {
            title: 'Community',
            items: [
              {
                label: 'GitHub',
                href: 'https://github.com/cbries/railhq.io'
              },
              {
                label: 'Instagram',
                href: 'https://www.instagram.com/railhq.io'
              },
              {
                label: 'YouTube',
                href: 'https://www.youtube.com/@railhqio'
              }
            ],
          },
          {
            title: 'More',
            items: [
              {
                label: 'Anleitungen',
                to: '/docs/intro',
              }
              // ,
              // {
              //   label: 'Blog',
              //   to: '/blog',
              // }              
            ],
          },
        ],
        copyright: `© ${new Date().getFullYear()} <a href="https://railhq.io/dashboard">RailHQ</a> – Open Source Community Project`,
      },
      prism: {
        theme: prismThemes.github,
        darkTheme: prismThemes.dracula,
      },
    }),
};

export default config;
