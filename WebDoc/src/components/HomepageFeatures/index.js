import clsx from 'clsx';
import Heading from '@theme/Heading';
import styles from './styles.module.css';

const FeatureList = [
  {
    title: 'Einfach zu bedienen',
    img: require('@site/static/img/railhq-hero1.png').default,
    description: (
      <>
        Unsere Software wurde speziell entwickelt, um dir die einfache Steuerung deiner Modelleisenbahn zu ermöglichen. 
        Mit unserem intuitiven Gleisplan-Editor kannst du sofort loslegen, Züge steuern und deine Lokomotiven nach 
        Belieben auswählen – alles in einer benutzerfreundlichen Oberfläche, die dir die komplexe Technik abnimmt.
      </>
    ),
  },
  {
    title: 'Gestalte nach deinen Wünschen',
    img: require('@site/static/img/railhq-hero5.png').default,
    description: (
      <>
        Unsere Software lässt dir die volle Kontrolle, während wir uns um die technischen Details kümmern. 
        Passen deine Lokomotivbilder direkt im Browser an – schneide sie präzise zu und optimiere sie für 
        deinen Gleisplan, ohne dass du zusätzliche Software benötigst. So kannst du deine Lokomotiven 
        individuell gestalten und sofort nutzen.
      </>
    ),
  },
  {
    title: 'Detaillierte Fahrtenprotokolle',
    img: require('@site/static/img/railhq-hero4.png').default,
    description: (
      <>
        Behalte den Überblick über deine Lokomotiven mit präzisen Fahrtenprotokollen. Unsere Software zeichnet 
        jede Fahrt auf, zeigt dir Pausen zwischen den Fahrten und stellt dir alle Daten sowohl grafisch 
        als auch tabellarisch zur Verfügung. Ideal für deine eigene Auswertung und um die Leistung 
        deiner Lokomotiven zu optimieren.
      </>
    ),
  },
];

function Feature({img, title, description}) {
  return (
    <div className={clsx('col col--4')}>
      <div className="text--center">
        <img src={img} alt={title} className={styles.featureImg} />
      </div>
      <div className="text--center padding-horiz--md">
        <Heading as="h3">{title}</Heading>
        <p>{description}</p>
      </div>
    </div>
  );
}

export default function HomepageFeatures() {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
