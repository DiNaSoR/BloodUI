import './PortraitShotPlaceholder.css';

interface PortraitShotPlaceholderProps {
  /** Title rendered in the placeholder (e.g. "Class Tab") */
  title: string;
  /** Optional short note (e.g. "Replace with your screenshot") */
  note?: string;
  /** Optional recommended resolution label (defaults to 1080×1920) */
  resolution?: string;
}

/**
 * 9:16 ratio placeholder meant to be replaced with real screenshots later.
 * We use CSS `aspect-ratio` so it stays responsive.
 */
export function PortraitShotPlaceholder({
  title,
  note = 'Replace this placeholder with your screenshot.',
  resolution = '1080×1920',
}: PortraitShotPlaceholderProps) {
  return (
    <figure className="portrait-shot">
      <div className="portrait-shot-frame" role="img" aria-label={`${title} screenshot placeholder`}>
        <div className="portrait-shot-content">
          <div className="portrait-shot-title">{title}</div>
          <div className="portrait-shot-meta">9:16 • {resolution}</div>
          <div className="portrait-shot-note">{note}</div>
        </div>
      </div>
      <figcaption className="portrait-shot-caption">
        Screenshot placeholder for <strong>{title}</strong>
      </figcaption>
    </figure>
  );
}

