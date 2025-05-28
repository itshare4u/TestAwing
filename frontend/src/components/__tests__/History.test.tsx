import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import History from '../History';

describe('History', () => {
    const mockHistoryItem = {
        id: 1,
        n: 3,
        m: 3,
        p: 3,
        minFuel: 4,
        createdAt: new Date().toISOString()
    };

    const defaultProps = {
        history: [mockHistoryItem],
        totalCount: 1,
        totalPages: 1,
        historyPage: 1,
        onHistoryPageChange: jest.fn(),
        onHistoryItemClick: jest.fn()
    };

    beforeEach(() => {
        jest.clearAllMocks();
    });

    it('renders history items correctly', () => {
        render(<History {...defaultProps} />);
        expect(screen.getByText(`${mockHistoryItem.n}×${mockHistoryItem.m}`)).toBeInTheDocument();
        expect(screen.getByText(`p=${mockHistoryItem.p}`)).toBeInTheDocument();
        expect(screen.getByText(`${mockHistoryItem.minFuel}`)).toBeInTheDocument();
    });

    it('shows empty state when no history items', () => {
        render(<History {...defaultProps} history={[]} />);
        expect(screen.getByText(/no solutions yet/i)).toBeInTheDocument();
    });

    it('calls onHistoryItemClick when row is clicked', () => {
        render(<History {...defaultProps} />);
        const row = screen.getByText(`${mockHistoryItem.n}×${mockHistoryItem.m}`).closest('tr');
        fireEvent.click(row!);
        expect(defaultProps.onHistoryItemClick).toHaveBeenCalledWith(mockHistoryItem);
    });

    it('shows total count when available', () => {
        render(<History {...defaultProps} totalCount={42} />);
        expect(screen.getByText(/total: 42 solutions/i)).toBeInTheDocument();
    });

    it('calls onHistoryPageChange when pagination changes', () => {
        render(<History {...defaultProps} totalPages={3} />);
        const nextPageButton = screen.getByRole('button', { name: /go to page 2/i });
        fireEvent.click(nextPageButton);
        expect(defaultProps.onHistoryPageChange).toHaveBeenCalledWith(2);
    });

    it('formats date correctly', () => {
        const date = new Date();
        render(<History {...defaultProps} history={[{...mockHistoryItem, createdAt: date.toISOString()}]} />);
        expect(screen.getByText(new RegExp(date.toLocaleDateString()))).toBeInTheDocument();
    });
});
