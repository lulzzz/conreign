'use strict';
import Promise from 'bluebird';
import identity from 'lodash/identity';
import defaults from 'lodash/defaults';
import isString from 'lodash/isString';
import isObject from 'lodash/isObject';
import isFunction from 'lodash/isFunction';
import extend from 'lodash/extend';

export const PROMISE_STATUSES = {
    PENDING: 'PENDING',
    RESOLVED: 'RESOLVED',
    REJECTED: 'REJECTED'
};

export const TYPE_SUFFIXES = {
    SUCCESS: '',
    ERROR: '_ERROR',
    PROMISE_PENDING: `_${PROMISE_STATUSES.PENDING}`,
    PROMISE_SUCCESS: `_${PROMISE_STATUSES.RESOLVED}`,
    PROMISE_ERROR: `_${PROMISE_STATUSES.REJECTED}`
};

function isPromiseLike(o) {
    return isObject(o) && isFunction(o.then);
}

function promiseIdentity(...args) {
    return Promise.resolve(args);
}

function ensureTypeIsSpecified(definition = {}) {
    if (!isString(definition.type) || definition.type.length === 0) {
        throw new Error('Action should have a type!');
    }
    return definition;
}

export function defineAction(definition = {}) {

    definition = ensureTypeIsSpecified(definition);

    definition = defaults(definition, {
        successType: `${definition.type}${TYPE_SUFFIXES.SUCCESS}`,
        errorType: `${definition.type}${TYPE_SUFFIXES.ERROR}`,
        mapPayload: identity,
        mapMeta: identity
    });

    function create(payload, meta) {
        payload = definition.mapPayload(payload);
        const isError = payload instanceof Error;
        const action = {
            type: isError ? definition.errorType : definition.successType,
            payload: payload,
            error: isError,
            meta: definition.mapMeta(meta)
        };
        return action;
    }

    create.error = function(err) {
        if (!err instanceof Error) {
            throw new Error('Only error objects should be passed to error action creators!');
        }
        return create(err);
    };

    create.$types = {
        success: definition.successType,
        error: definition.errorType
    };

    return create;
}

export function definePromiseAction(definition = {}) {

    definition = ensureTypeIsSpecified(definition);

    definition = defaults(definition, {
        pendingType: `${definition.type}${TYPE_SUFFIXES.PROMISE_PENDING}`,
        successType: `${definition.type}${TYPE_SUFFIXES.PROMISE_SUCCESS}`,
        errorType: `${definition.type}${TYPE_SUFFIXES.PROMISE_ERROR}`,
        mapPayload: promiseIdentity,
        mapMeta: identity
    });

    // Mark all actions as async promise actions
    const mapMeta = (meta, status, state) => extend({}, definition.mapMeta(meta, state), {
        $async: {method: 'promise', status: status}
    });

    function create(payload, meta) {
        // Passing to the thunk middleware
        return (dispatch, getState) => {
            const payloadPromise = definition.mapPayload(payload, getState());
            if (!isPromiseLike(payloadPromise)) {
                throw new Error('Promise actions should create promises for a payload!');
            }
            dispatch({
                type: definition.pendingType,
                // We should only pass serializable values in actions
                payload: isPromiseLike(payload) ? {} : payload,
                error: false,
                meta: mapMeta(meta, PROMISE_STATUSES.PENDING, getState())
            });

            function onSuccess(result) {
                return dispatch({
                    type: definition.successType,
                    payload: result,
                    error: false,
                    meta: mapMeta(meta, PROMISE_STATUSES.RESOLVED, getState())
                });
            }

            function onError(error) {

                return dispatch({
                    type: definition.errorType,
                    payload: error,
                    error: true,
                    meta: mapMeta(meta, PROMISE_STATUSES.REJECTED, getState())
                });
            }

            return payloadPromise.then(onSuccess, onError);
        };
    }

    create.$types = {
        pending: definition.pendingType,
        success: definition.successType,
        error: definition.errorType
    };

    return create;
}

export default {
    defineAction: defineAction,
    definePromiseAction: definePromiseAction
}