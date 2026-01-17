import type { MDXComponents } from 'mdx/types';
import { CodeBlock } from './CodeBlock';
import { Callout } from './Callout';
import { PortraitShotPlaceholder } from './PortraitShotPlaceholder';

/**
 * Custom MDX components that override default markdown rendering
 */
export const mdxComponents: MDXComponents = {
  // Override pre/code for syntax highlighting
  pre: ({ children, ...props }) => {
    // Extract code element and its props
    const codeElement = children as React.ReactElement;
    if (codeElement?.props?.className) {
      const language = codeElement.props.className.replace('language-', '');
      return (
        <CodeBlock language={language} {...props}>
          {codeElement.props.children}
        </CodeBlock>
      );
    }
    return <pre {...props}>{children}</pre>;
  },

  // Custom components available in MDX
  Callout,
  CodeBlock,
  PortraitShotPlaceholder,

  // Style overrides for headings with anchor links
  h2: ({ children, id, ...props }) => (
    <h2 id={id} {...props}>
      <a href={`#${id}`} className="anchor-link">
        {children}
      </a>
    </h2>
  ),
  h3: ({ children, id, ...props }) => (
    <h3 id={id} {...props}>
      <a href={`#${id}`} className="anchor-link">
        {children}
      </a>
    </h3>
  ),
};
