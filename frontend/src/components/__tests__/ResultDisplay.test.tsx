import React from 'react';
import { render, screen } from '@testing-library/react';
import ResultDisplay from '../ResultDisplay';

describe('ResultDisplay', () => {
    it('displays result when provided', () => {
        render(<ResultDisplay result={42} error="" />);
        expect(screen.getByText(/minimum fuel required: 42/i)).toBeInTheDocument();
    });

    it('displays nothing when result is null', () => {
        render(<ResultDisplay result={null} error="" />);
        expect(screen.queryByText(/minimum fuel required/i)).not.toBeInTheDocument();
    });

    it('displays error message when error is provided', () => {
        const errorMessage = 'Test error message';
        render(<ResultDisplay result={null} error={errorMessage} />);
        expect(screen.getByText(errorMessage)).toBeInTheDocument();
    });

    it('displays both result and success message when result is provided', () => {
        render(<ResultDisplay result={10} error="" />);
        expect(screen.getByText(/minimum fuel required: 10/i)).toBeInTheDocument();
        expect(screen.getByText(/treasure hunt has been solved/i)).toBeInTheDocument();
    });

    it('uses correct color scheme for success message', () => {
        render(<ResultDisplay result={10} error="" />);
        const card = screen.getByText(/minimum fuel required: 10/i).closest('.MuiCard-root');
        expect(card).toHaveStyle({ backgroundColor: expect.stringContaining('success.light') });
    });
});
