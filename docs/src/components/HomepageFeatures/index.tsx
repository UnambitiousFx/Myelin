import type {CSSProperties, ReactNode} from 'react';
import Heading from '@theme/Heading';
import styles from './styles.module.css';

type FeatureItem = {
  icon: string;
  accent: string;
  title: string;
  description: ReactNode;
};

const FeatureList: FeatureItem[] = [
  {
    icon: '🧱',
    accent: '#512bd4',
    title: 'Public addresses only',
    description: (
      <>
        Every special-purpose range of the IANA registries is refused: private,
        loopback, link-local and cloud metadata, mapped and NAT64 forms included.
      </>
    ),
  },
  {
    icon: '🔁',
    accent: '#2563eb',
    title: 'DNS rebinding proof',
    description: (
      <>
        Myelin resolves the host itself and connects only to the addresses it
        checked; a second DNS answer is never what the socket uses.
      </>
    ),
  },
  {
    icon: '↪️',
    accent: '#d97706',
    title: 'Redirects revalidated',
    description: (
      <>
        Each hop of a GET is checked like the first request; a POST or a request
        carrying credentials is never redirected.
      </>
    ),
  },
  {
    icon: '⏱️',
    accent: '#2e8555',
    title: 'Bounded and quiet',
    description: (
      <>
        Time and body size are capped, and a refusal is logged with its host and
        reason only, never a path that may hold a secret.
      </>
    ),
  },
];

function Feature({icon, accent, title, description}: FeatureItem) {
  return (
    <div className={styles.featureCard} style={{'--card-accent': accent} as CSSProperties}>
      <div className={styles.featureIconWrap}>
        <span className={styles.featureIcon}>{icon}</span>
      </div>
      <Heading as="h3" className={styles.featureTitle}>{title}</Heading>
      <p className={styles.featureDescription}>{description}</p>
    </div>
  );
}

export default function HomepageFeatures(): ReactNode {
  return (
    <section className={styles.features}>
      <div className="container">
        <Heading as="h2" className={styles.sectionHeading}>Why Myelin?</Heading>
        <div className={styles.featuresGrid}>
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
