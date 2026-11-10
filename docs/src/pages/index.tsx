import type {ReactNode} from 'react';
import clsx from 'clsx';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import Heading from '@theme/Heading';
import HomepageFeatures from '@site/src/components/HomepageFeatures';

import styles from './index.module.css';

const QUICK_START = `using UnambitiousFx.Myelin;

// 1 · Register
services.AddMyelin(o =>
{
    o.AllowedHosts = ["api.example.com", "*.cdn.example.org"];
    o.UserAgent = "MyApp/1.0";
});

// 2 · Fetch a URL you do not control
var response = await http.GetAsync(url, maxBytes: 512_000, ct);

// 3 · Or harden an IHttpClientFactory client
services.AddHttpClient<WebhookSender>().UseMyelin();

// https://169.254.169.254/...  -> address_refused
// https://localhost/admin       -> address_refused
// http://example.com/           -> not_https`;

function HomepageHeader() {
  const {siteConfig} = useDocusaurusContext();
  return (
    <header className={styles.heroBanner}>
      <div className={clsx('container', styles.heroInner)}>
        <div className={styles.heroLeft}>
          <Heading as="h1" className="hero__title" style={{color: 'inherit', margin: 0}}>
            {siteConfig.title}
          </Heading>
          <p className={styles.heroTagline}>{siteConfig.tagline}</p>
          <div className={styles.heroButtons}>
            <Link className="button button--primary button--lg" to="/docs/getting-started">
              Get Started →
            </Link>
            <Link
              className="button button--outline button--lg"
              style={{color: 'inherit', borderColor: 'rgba(255,255,255,0.4)'}}
              href="https://github.com/UnambitiousFx/Myelin">
              View on GitHub
            </Link>
          </div>
          <div className={styles.heroInstall}>
            <code>dotnet add package UnambitiousFx.Myelin</code>
          </div>
        </div>

        <div className={styles.heroCode}>
          <div className={styles.heroCodeLabel}>Quick start</div>
          <pre>{QUICK_START}</pre>
        </div>
      </div>
    </header>
  );
}

type NextStepItem = {
  emoji: string;
  title: string;
  description: string;
  to: string;
};

const NEXT_STEPS: NextStepItem[] = [
  {
    emoji: '🚀',
    title: 'Getting Started',
    description: 'Install the package and send your first SSRF-safe request.',
    to: '/docs/getting-started',
  },
  {
    emoji: '🧱',
    title: 'Address policy',
    description: 'Every address range Myelin refuses, and why.',
    to: '/docs/address-policy',
  },
  {
    emoji: '🏭',
    title: 'IHttpClientFactory',
    description: 'Harden a typed or named HttpClient with UseMyelin().',
    to: '/docs/http-client-factory',
  },
];

export default function Home(): ReactNode {
  const {siteConfig} = useDocusaurusContext();
  return (
    <Layout
      title={siteConfig.title}
      description="SSRF-safe outbound HTTP for .NET: public addresses only, DNS rebinding included, every redirect revalidated, bounded time and size.">
      <HomepageHeader />
      <main>
        <HomepageFeatures />
        <section className={styles.nextSteps}>
          <div className="container">
            <Heading as="h2" className="text--center">Explore the docs</Heading>
            <div className={styles.nextStepsGrid}>
              {NEXT_STEPS.map((item) => (
                <Link key={item.to} to={item.to} style={{textDecoration: 'none'}}>
                  <div
                    style={{
                      background: 'var(--myelin-card-bg)',
                      border: '1px solid var(--myelin-card-border)',
                      borderRadius: '10px',
                      padding: '1.25rem',
                      height: '100%',
                      transition: 'box-shadow 0.2s, transform 0.2s',
                    }}
                    onMouseEnter={e => {
                      (e.currentTarget as HTMLDivElement).style.transform = 'translateY(-2px)';
                      (e.currentTarget as HTMLDivElement).style.boxShadow = 'var(--myelin-card-shadow-hover)';
                    }}
                    onMouseLeave={e => {
                      (e.currentTarget as HTMLDivElement).style.transform = '';
                      (e.currentTarget as HTMLDivElement).style.boxShadow = '';
                    }}>
                    <div style={{fontSize: '1.75rem', marginBottom: '0.5rem'}}>{item.emoji}</div>
                    <Heading as="h3" style={{fontSize: '1rem', marginBottom: '0.4rem'}}>
                      {item.title}
                    </Heading>
                    <p style={{fontSize: '0.875rem', margin: 0, opacity: 0.75}}>{item.description}</p>
                  </div>
                </Link>
              ))}
            </div>
          </div>
        </section>
      </main>
    </Layout>
  );
}
