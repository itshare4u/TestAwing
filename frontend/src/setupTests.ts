import '@testing-library/jest-dom';

// Mock window.ResizeObserver
class ResizeObserverMock {
    observe() {}
    unobserve() {}
    disconnect() {}
}

window.ResizeObserver = ResizeObserverMock;

// Mock window.scrollTo
window.scrollTo = jest.fn();

// Mock intersection observer
class IntersectionObserverMock {
    observe() {}
    unobserve() {}
    disconnect() {}
}

window.IntersectionObserver = IntersectionObserverMock;
