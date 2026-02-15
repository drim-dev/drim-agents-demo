interface ContainerProps {
  children: React.ReactNode;
  className?: string;
}

export function ReadingContainer({ children, className = '' }: ContainerProps) {
  return (
    <div className={`max-w-4xl mx-auto px-2 sm:px-6 lg:px-8 ${className}`}>
      {children}
    </div>
  );
}

export function DataContainer({ children, className = '' }: ContainerProps) {
  return (
    <div className={`max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 ${className}`}>
      {children}
    </div>
  );
}

export function FormContainer({ children, className = '' }: ContainerProps) {
  return (
    <div className={`max-w-2xl mx-auto px-4 sm:px-6 lg:px-8 ${className}`}>
      {children}
    </div>
  );
}

export function AuthContainer({ children, className = '' }: ContainerProps) {
  return (
    <div className={`max-w-md mx-auto px-4 sm:px-6 lg:px-8 ${className}`}>
      {children}
    </div>
  );
}
