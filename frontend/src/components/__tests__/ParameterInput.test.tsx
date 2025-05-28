import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import ParameterInput from '../ParameterInput';

describe('ParameterInput', () => {
    const mockProps = {
        n: 3,
        m: 3,
        p: 3,
        loading: false,
        onNChange: jest.fn(),
        onMChange: jest.fn(),
        onPChange: jest.fn(),
        onCreateMatrix: jest.fn(),
        onGenerateRandom: jest.fn(),
        onLoadExample: jest.fn(),
        onFileUpload: jest.fn(),
        onExportMatrix: jest.fn(),
        onSolve: jest.fn()
    };

    beforeEach(() => {
        jest.clearAllMocks();
    });

    it('renders all input fields with correct values', () => {
        render(<ParameterInput {...mockProps} />);
        
        const nInput = screen.getByLabelText(/rows/i);
        const mInput = screen.getByLabelText(/columns/i);
        const pInput = screen.getByLabelText(/max chest/i);

        expect(nInput).toHaveValue(3);
        expect(mInput).toHaveValue(3);
        expect(pInput).toHaveValue(3);
    });

    it('calls appropriate handlers when input values change', () => {
        render(<ParameterInput {...mockProps} />);

        const nInput = screen.getByLabelText(/rows/i);
        fireEvent.change(nInput, { target: { value: '4' } });
        expect(mockProps.onNChange).toHaveBeenCalledWith(4);

        const mInput = screen.getByLabelText(/columns/i);
        fireEvent.change(mInput, { target: { value: '5' } });
        expect(mockProps.onMChange).toHaveBeenCalledWith(5);

        const pInput = screen.getByLabelText(/max chest/i);
        fireEvent.change(pInput, { target: { value: '6' } });
        expect(mockProps.onPChange).toHaveBeenCalledWith(6);
    });

    it('calls createMatrix when Create Matrix button is clicked', () => {
        render(<ParameterInput {...mockProps} />);
        const createButton = screen.getByText(/create matrix/i);
        fireEvent.click(createButton);
        expect(mockProps.onCreateMatrix).toHaveBeenCalled();
    });

    it('calls generateRandom when Random button is clicked', () => {
        render(<ParameterInput {...mockProps} />);
        const randomButton = screen.getByText(/random/i);
        fireEvent.click(randomButton);
        expect(mockProps.onGenerateRandom).toHaveBeenCalled();
    });

    it('calls loadExample with correct index when example buttons are clicked', () => {
        render(<ParameterInput {...mockProps} />);
        const example1Button = screen.getByText(/example 1/i);
        const example2Button = screen.getByText(/example 2/i);
        const example3Button = screen.getByText(/example 3/i);

        fireEvent.click(example1Button);
        expect(mockProps.onLoadExample).toHaveBeenCalledWith(1);

        fireEvent.click(example2Button);
        expect(mockProps.onLoadExample).toHaveBeenCalledWith(2);

        fireEvent.click(example3Button);
        expect(mockProps.onLoadExample).toHaveBeenCalledWith(3);
    });

    it('disables buttons when loading', () => {
        render(<ParameterInput {...mockProps} loading={true} />);
        const solveButton = screen.getByText(/solve/i);
        const randomButton = screen.getByText(/random/i);
        
        expect(solveButton).toBeDisabled();
        expect(randomButton).toBeDisabled();
    });

    it('constrains n and m inputs to maximum of 500', () => {
        render(<ParameterInput {...mockProps} />);
        const nInput = screen.getByLabelText(/rows/i);
        const mInput = screen.getByLabelText(/columns/i);

        expect(nInput).toHaveAttribute('max', '500');
        expect(mInput).toHaveAttribute('max', '500');
    });
});
